using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Tools.Extensions;
using Tools.Markers;
using Traffic.Actions;
using Traffic.Actions.Base;
using Traffic.Cars;

namespace Traffic.Drivers
{
    internal abstract class Driver
    {
        //------------------------------------------------------------------
        protected List<Actions.Base.Action> Actions = new List<Actions.Base.Action> ();
        protected List<Actions.Base.Action> ActionsToAdd = new List<Actions.Base.Action> ();
        protected float DangerousZone;

        //------------------------------------------------------------------
        public Car Car { get; set; }
        public float Velocity { get; set; }

        //------------------------------------------------------------------
        protected Driver (Car car)
        {
            Car = car;

            Add (new Shrink (this));
        }

        #region Actions

        //-----------------------------------------------------------------
        public virtual void Update (float elapsed)
        {
            Actions.ForEach (action => action.Update (elapsed));

            AddDeferredActions ();

            Actions.RemoveAll (actions => actions.Finished);
        }

        //------------------------------------------------------------------
        private void AddDeferredActions ()
        {
            // Insert in first position to detect Lock property as early as possible
//            ActionsToAdd.ForEach (action => Actions.Insert (0, action));

            Actions.AddRange (ActionsToAdd);
            ActionsToAdd.Clear ();
        }

        //------------------------------------------------------------------
        public void Add (Actions.Base.Action action)
        {
            ActionsToAdd.Add (action);
        }

        #endregion

        #region Sensor Analysis

        //------------------------------------------------------------------
        public void CalculateDangerousZone ()
        {
            DangerousZone = Car.Lenght * Car.Velocity / 60.0f;
//            new Line (Car.GlobalPosition, Car.GlobalPosition + new Vector2 (0, -DangerousZone));
        }

        //------------------------------------------------------------------
        public float Distance (Car car)
        {
            // Don't react with own Car
            if (car == null  || car == Car) return float.MaxValue;

            var distance = Car.GlobalPosition - car.GlobalPosition;

            return Math.Abs (distance.Y);
        }

        //------------------------------------------------------------------
        public Car FindClosestCar (IEnumerable<Car> cars)
        {
            return cars.MinBy (Distance);
        }

        //------------------------------------------------------------------
        protected float GetMinimumDistance (IEnumerable <Car> cars)
        {
            if (!cars.Any ()) return float.MaxValue;

            return cars.Min <Car, float> (Distance);
        }

        //------------------------------------------------------------------
        public bool IsAhead (Car car)
        {
            return car.GlobalPosition.Y < Car.GlobalPosition.Y;
        }

        //------------------------------------------------------------------
        public bool CheckLane (Lane lane)
        {
            if (lane == null) return false;

            if (GetMinimumDistance (lane.Cars) < DangerousZone)
                return false;

            return true;
        }

        #endregion

        #region Car Controll

        //------------------------------------------------------------------
        public void Brake (Composite action, int times)
        {
            action.Add (new Repeated (Car.Brake, times) {Name = "Brake"});
        }

        //------------------------------------------------------------------
        public void Accelerate (Composite action, int times)
        {
            if (Car.Velocity < Velocity)
                action.Add (new Repeated (Car.Accelerate, times) { Name = "Accelerate" });
        }

        //------------------------------------------------------------------
        public void EnableBlinker (Lane lane, Composite action)
        {
            Car.EnableBlinker (lane);

            action.Add (new Sleep (0.5f));
            action.Add (new Generic (() => Car.DisableBlinker ()));
        }

        //------------------------------------------------------------------
        public bool TryChangeLane (Lane lane, Composite action)
        {
            if (CheckLane (lane))
            {
                EnableBlinker (lane, action);
                ChangeLane (lane, action);
                return true;
            }

            return false;
        }

        //------------------------------------------------------------------
        public void ChangeLane (Lane lane, Composite action)
        {
            if (lane == null) return;

            // No Lane changing when car doesn't move
            if (Car.Velocity < 10) return;

            float duration = 200.0f / Car.Velocity;

            // Rotate
            Action <float> rotate = share => Car.Angle += share;
            float finalAngle = MathHelper.ToRadians ((lane.GlobalPosition.X < Car.GlobalPosition.X) ? -10 : 10);
            action.Add (new Controller (rotate, finalAngle, duration * 0.1f));

            // Moving
            Action <Vector2> move = shift => Car.Position += shift;
            var diapason = new Vector2 (lane.GlobalPosition.X - Car.GlobalPosition.X, 0);
            action.Add (new Controller (move, diapason, duration * 0.2f));

            // Inverse rotating
            var inverseRotating = new Controller (rotate, -finalAngle, duration * 0.1f);
            action.Add (inverseRotating);

            // Add to new Lane
            action.Add (new Generic (() => lane.Add (Car)));
        }

        #endregion
    }
}