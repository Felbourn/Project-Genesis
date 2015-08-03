using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Felbourn
{
    public class ModuleGravityBreakup : PartModule
    {
        //-----------------------------------------------------------------------------------------
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Health")]
        public float health = 200;
        [KSPField]
        public float threshold = 0.32f;
        [KSPField]
        public float damageRate = 0.2f;
        [KSPField]
        public float pressureMin = 0.5f;
        [KSPField]
        public float minTemp = 400;
        [KSPField]
        public float maxTemp = 800;
        [KSPField]
        public float maxLossTo = 700;
        [KSPField]
        public float maxTempFactor = 0.75f;

        private bool logged = false;
        private bool broken = false;

        //-----------------------------------------------------------------------------------------
        public override void OnStart(PartModule.StartState state)
        {
            // prevent divide by zero errors
            if (part.maxTemp <= 0)
                part.maxTemp = minTemp;
            if (part.skinMaxTemp <= 0)
                part.skinMaxTemp = minTemp;

            // skin will heat up before the part, so save effort and just check skins
            maxTemp = (float)(part.skinMaxTemp * maxTempFactor);
        }

        //-----------------------------------------------------------------------------------------
        public virtual void FixedUpdate()
        {
            if (vessel != null)
            {
                if (vessel.HoldPhysics)
                    return;
                if (vessel.dynamicPressurekPa < pressureMin)
                    return;
            }
            else
            {
                if (part.dynamicPressurekPa < pressureMin)
                    return;
            }

            float heating = (float)Math.Pow(part.skinTemperature / maxTemp, 2);
            if (heating < threshold)
                return;

            if (broken)
            {
                // slowly degrade the part, linear so that it's not instant
                if (part.maxTemp > maxLossTo)
                    part.maxTemp -= damageRate;
                if (part.skinMaxTemp > maxLossTo)
                    part.skinMaxTemp -= damageRate;
            }
            else
            {
                if (!logged)
                {
                    FlightLog(part.partInfo.title + " is taking heat damage!");
                    logged = true;
                }
                health -= heating;
                if (health > 0)
                    return;

                FlightLog(part.partInfo.title + " is melting and broke off!");
                part.disconnect();
                broken = true;
            }
        }

        private void FlightLog(string message)
        {
            FlightLogger.eventLog.Add(String.Format("[{0:D2}:{1:D2}:{2:D2}] " + message, FlightLogger.met_hours, FlightLogger.met_mins, FlightLogger.met_secs));
        }
    }
}
