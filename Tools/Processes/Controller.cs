﻿using System;
using Microsoft.Xna.Framework;

namespace Tools.Processes
{
    public class Controller : Process
    {
        private readonly dynamic action;
        private readonly dynamic diapason;

        //------------------------------------------------------------------
        public Controller (dynamic action, dynamic diapason, float duration) : base (duration)
        {
            this.action = action;
            this.diapason = diapason;
        }

        //------------------------------------------------------------------
        public override void Update (float elapsed)
        {
            base.Update (elapsed);

            float fraction = elapsed / Duration;

            action.Invoke (fraction * diapason);
        }
    }
}