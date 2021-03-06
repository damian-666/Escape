using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Tools.Markers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Traffic.Cars;
using Traffic.Cars.Weights;

namespace Traffic
{
    public class Lane : Engine.Object
    {
        public enum Weight
        {
            Light,
            Medium,
            Heavy
        }

        //------------------------------------------------------------------
        private int height;
        private readonly List <Car> newCars;
        private static int carsCounter;

        //------------------------------------------------------------------
        public const int MinimumCars = 3;
        public const int MaximumCars = Settings.MaximumCarsOnLane;

        //------------------------------------------------------------------
        public readonly int ID;
        private int border;
        public List <Car> Cars { get; private set; }
        public int Velocity { get; set; }
        public Lane Left { get; set; }
        public Lane Right { get; set; }
        public static Random Random { get; set; }
        public Road Road { get; private set; }
        public int CarsQuantity { get; set; }

        #region Creation

        //------------------------------------------------------------------
        static Lane ()
        {
            Random = new Random ();
        }

        //------------------------------------------------------------------
        public Lane (Road road, int id) : base (road)
        {
            ID = id;
            Road = road;
            Fixed = true;

            CalculatePosition ();
            CalculateVelocity ();

            Cars = new List <Car> ();
            newCars = new List <Car> ();
        }

        //------------------------------------------------------------------
        private void CalculateVelocity ()
        {
            const int maximumVelocity = 240;
            const int step = 20;

            Velocity = maximumVelocity - ID * step;
        }

        //------------------------------------------------------------------
        private void CalculatePosition ()
        {
            const int laneWidth = 40;
            int position = ID * laneWidth + laneWidth / 2;

            LocalPosition = new Vector2 (position, 0);
        }

        //------------------------------------------------------------------
        public override void Setup (Game game)
        {
            height = Road.Game.GraphicsDevice.Viewport.Height;
            border = height;

            base.Setup (game);
        }

        //------------------------------------------------------------------
        protected Car CreateCar ()
        {
            var car = new Car (this, carsCounter, GetInsertionPosition ());
            SetupCar (car);

            return car;
        }

        //------------------------------------------------------------------
        public Player CreatePlayer (Game game)
        {
            var player = new Player (this, carsCounter, 400);
            SetupCar (player);

            return player;
        }

        //------------------------------------------------------------------
        public Police CreatePolice (Game game)
        {
            var police = new Police (this, carsCounter, Settings.PoliceStartPosition);
            SetupCar (police);

            return police;
        }

        //-----------------------------------------------------------------
        private void SetupCar (Car car)
        {
            car.Setup (Road.Game);
            AcceptCar (car);

            carsCounter++;
        }

        //------------------------------------------------------------------
        public Weight GetWeight ()
        {
            if (ID < Road.LanesQuantity / 3) 
                return Weight.Light;
            if (ID < Road.LanesQuantity * 2 / 3)
                return Weight.Medium;
            if (ID < Road.LanesQuantity)
                return Weight.Heavy;

            throw new NotSupportedException();
        }

        //------------------------------------------------------------------
        private int GetInsertionPosition ()
        {
            // Determine where place car: above Player or bottom
            float playerVelocity = (Road.Player != null) ? Road.Player.Velocity : 0;
            int sign = (Velocity > playerVelocity) ? 1 : -1;

            return GetFreePositionOutsideScreen (sign);
        }

        //------------------------------------------------------------------
        private int GetFreePositionOutsideScreen (int sign)
        {
            int position = 0;
            int lower = 0 + sign * border;
            int upper = height + sign * border;

            // Get free position for 20 iterations
            foreach (var index in Enumerable.Range (0, 20))
            {
//                if (index == 19) Console.WriteLine ("No Space");
                    
                position = Random.Next (lower, upper);

                if (!Cars.Any ()) break;

                float minimum = Cars.Min (car => Math.Abs (car.Position.Y - position));

                if (minimum > 150) break;
            }

            return position;
        }

        #endregion

        //------------------------------------------------------------------
        public override void Update (float elapsed)
        {
            base.Update (elapsed);

            AcceptNewCars ();

            CleanUp ();
            AppendCars ();

            Debug ();
        }

        #region Cars

        //------------------------------------------------------------------
        private void AppendCars ()
        {
            if (Settings.NoCars) return;

            if (Cars.Count < CarsQuantity)
                CreateCar ();
        }

        //------------------------------------------------------------------
        private void CleanUp ()
        {
            // Remove Cars outside the screen
            Cars.RemoveAll (car =>
            {
                int position = (int) car.Position.Y;
                return position < -border || position > height + border;
            });

            // Remove all dead Cars
            Cars.RemoveAll (car => car.Deleted);
        }

        //------------------------------------------------------------------
        private void AcceptNewCars ()
        {
            newCars.ForEach (AcceptCar);
            newCars.Clear ();
        }

        //------------------------------------------------------------------
        public void Add (Car car)
        {
            newCars.Add (car);
        }

        //------------------------------------------------------------------
        private void AcceptCar (Car car)
        {
            // Remove Car from the previous Lane
            if (car.Lane != this)
                car.Lane.Cars.Remove (car);

            // Add Car to the current Lane
            Cars.Add (car);

            // Implement smooth crossing from the previous Lane to the current one
            car.LocalPosition = new Vector2 (car.Position.X - Position.X, car.Position.Y);

            car.SetLane (this);
        }

        #endregion

        //------------------------------------------------------------------
        public override string ToString ()
        {
            return ID.ToString();
//            return string.Format ("{0}", ID);
        }

        //------------------------------------------------------------------
        private void Debug ()
        {
//            new Text (ToString(), LocalPosition, Color.Orange);
//            new Text (Velocity.ToString ("F0"), LocalPosition);
//            new Text (CarsQuantity.ToString (), LocalPosition);

//            // Particular Type counter
//            int number = Cars.OfType <Player> ().Count ();
//            if (number != 0) 
//                new Text (number.ToString (""), LocalPosition);
        }
    }
}