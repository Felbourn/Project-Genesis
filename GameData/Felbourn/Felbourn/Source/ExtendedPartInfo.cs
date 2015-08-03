using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TweakScale;
using RemoteTech.Modules;

namespace ExtendedPartInfo
{
    public class ExtendedPartInfo : PartModule //, TweakScale.IRescalable<ExtendedPartInfo>
    {
        KSPAssembly remoteTech = new KSPAssembly("RemoteTech", 1, 6);

        // all

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Part")]
        private string PartName = "";
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        private string Internals = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiFormat = "N0", guiUnits = " kg")]
        private float Mass = 0;
        [KSPField(isPersistant = false)]
        public float OriginalMass = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private bool Shielded = false;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private string Temp = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private string SkinTemp = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private bool CrossFeed = false;

        // engines

        [KSPField(isPersistant = false, guiActive = true)]
        private float Thrust = 0;

        // tolerances

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private float CrashTolerance = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private float BreakForce = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private float BreakTorque = 0;

        // gyros

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private float GyroTorque = 0;

        // decouplers

        [KSPField(isPersistant = false, guiActive = true)]
        private float DecoupleForce = 0;

        private ModuleRCS myRCS = null;
        private ModuleEngines myEngine = null;
        private ModuleReactionWheel myCMG = null;
        private ModuleDecouple myDecoupler1 = null;
        private ModuleAnchoredDecoupler myDecoupler2 = null;

        //-----------------------------------------------------------------------------------------
        // interface
        public override void OnStart(PartModule.StartState state)
        {
            for (int i = part.Modules.Count - 1; i >= 0; i--)
            {
                if ((part.Modules[i] as ModuleRCS) != null)
                    myRCS = part.Modules[i] as ModuleRCS;

                if ((part.Modules[i] as ModuleEngines) != null)
                    myEngine = part.Modules[i] as ModuleEngines;

                if ((part.Modules[i] as ModuleReactionWheel) != null)
                    myCMG = part.Modules[i] as ModuleReactionWheel;

                if ((part.Modules[i] as ModuleDecouple) != null)
                    myDecoupler1 = part.Modules[i] as ModuleDecouple;

                if ((part.Modules[i] as ModuleAnchoredDecoupler) != null)
                    myDecoupler2 = part.Modules[i] as ModuleAnchoredDecoupler;
            }

            Display();

            PartName = part.partInfo.name;
            if (part.CrewCapacity > 0)
                Internals = part.InternalModelName + " for " + part.CrewCapacity;
            else
                DisableField("Internals");

            if (myDecoupler1)
                DecoupleForce = myDecoupler1.ejectionForce;
            else if (myDecoupler2)
                DecoupleForce = myDecoupler2.ejectionForce;
            else
                DisableField("DecoupleForce");
        }
        public override void OnUpdate()
        {
            Display();
        }

        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            StartCoroutine(DelayDisplay());
        }
        private System.Collections.IEnumerator DelayDisplay()
        {
            yield return null;
            Display();
        }

        private void Display()
        {
            Mass = (part.mass + part.GetResourceMass()) * 1000;
            CrossFeed = part.fuelCrossFeed;
            Temp = Math.Round(part.temperature) + " / " + part.maxTemp;
            SkinTemp = Math.Round(part.skinTemperature) + " / " + part.skinMaxTemp + " (" + IntWithUnits((float)part.thermalConductionFlux) + "," + IntWithUnits((float)part.thermalRadiationFlux) + ")";
            Shielded = part.ShieldedFromAirstream;

            if (myRCS)
                Thrust = myRCS.thrusterPower;
            else if (myEngine)
                Thrust = myEngine.maxThrust;
            else
                DisableField("Thrust");

            CrashTolerance = part.crashTolerance;
            BreakForce = part.breakingForce;
            BreakTorque = part.breakingTorque;

            if (myCMG)
                GyroTorque = myCMG.PitchTorque;
            else
                DisableField("GyroTorque");
        }

        //-----------------------------------------------------------------------------------------
        public override string GetInfo()
        {
            string partName = "Part name: " + part.partInfo.name + "\n";
            string path = "Path: " + part.partInfo.partUrl + "\n";

            string massInfo = "Original mass: ";
            if (OriginalMass < 1)
                massInfo += OriginalMass * 1000 + " kg\n";
            else
                massInfo += OriginalMass + " tons\n";
            
            string remoteTech = "";

            ModuleRTAntenna antenna = null;
            for (int i = part.Modules.Count - 1; i >= 0; i--)
                if ((part.Modules[i] as ModuleRTAntenna) != null)
                    antenna = part.Modules[i] as ModuleRTAntenna;

            //Debug.Log("name = " + part.partInfo.name);
            //Debug.Log("thermalMassModifier = " + part.thermalMassModifier);
            //Debug.Log("skinInternalConductionMult = " + part.skinInternalConductionMult);
            //Debug.Log("skinMassPerArea = " + part.skinMassPerArea);

            if (antenna)
            {

                remoteTech = "RemoteTech pairings:\n";

                float range = Math.Max(antenna.Mode1OmniRange, antenna.Mode1DishRange);
                float maxRange = (antenna.Mode1OmniRange != 0) ? antenna.Mode1OmniRange * 100 : antenna.Mode1DishRange * 1000;

                Debug.Log("[EP] omni = " + antenna.Mode1OmniRange);
                Debug.Log("[EP] dish = " + antenna.Mode1DishRange);
                Debug.Log("[EP] range = " + range);
                remoteTech += "   75km: " + RemoteTech(range, KM(75), maxRange) + "\n";
                remoteTech += "   250km: " + RemoteTech(range, KM(250), maxRange) + "\n";
                remoteTech += "   1Mm: " + RemoteTech(range, MM(1), maxRange) + "\n";
                remoteTech += "   3.5Mm: " + RemoteTech(range, MM(3.5f), maxRange) + "\n";
                remoteTech += "   8Mm: " + RemoteTech(range, MM(8), maxRange) + "\n";
                remoteTech += "   20Mm: " + RemoteTech(range, MM(20), maxRange) + "\n";
                remoteTech += "   45Mm: " + RemoteTech(range, MM(45), maxRange) + "\n";
                remoteTech += "   8Gm: " + RemoteTech(range, GM(8), maxRange) + "\n";
                remoteTech += "   15Gm: " + RemoteTech(range, GM(15), maxRange) + "\n";
                remoteTech += "   32Gm: " + RemoteTech(range, GM(32), maxRange) + "\n";
                remoteTech += "   93Gm: " + RemoteTech(range, GM(93), maxRange) + "\n";
            }
            return partName + path + massInfo + remoteTech;
        }

        //-----------------------------------------------------------------------------------------
        private string RemoteTech(float range1, float range2, float maxRange)
        {
            return DistWithUnits(Math.Min((float)(Math.Min(range1, range2) + Math.Sqrt(range1 * range2)), maxRange));
        }

        //-----------------------------------------------------------------------------------------
        private string DistWithUnits(float value)
        {
            if (value <= 1000)
                return value + "m";
            if (value <= 1000000)
                return Math.Round(value / 1000, 3) + "km";
            if (value <= 1000000000)
                return Math.Round(value / 1000000, 2) + "Mm";
            if (value <= 1000000000000)
                return Math.Round(value / 1000000000, 1) + "Gm";
            return value.ToString();
        }

        //-----------------------------------------------------------------------------------------
        private string IntWithUnits(float value)
        {
            if (value < 1000 && value > -1000)
                return Math.Round(value, 0).ToString();
            if (value <= 1000000 && value >= 1000000)
                return Math.Round(value / 1000, 0) + "K";
            if (value <= 1000000000 && value >= 1000000000)
                return Math.Round(value / 1000000, 0) + "M";
            if (value <= 1000000000000 && value >= 1000000000000)
                return Math.Round(value / 1000000000, 0) + "G";
            return value.ToString();
        }

        //-----------------------------------------------------------------------------------------
        private float KM(float value)
        {
            return value * 1000;
        }
        private float MM(float value)
        {
            return value * 1000000;
        }
        private float GM(float value)
        {
            return value * 1000000000;
        }

        //-----------------------------------------------------------------------------------------
        void DisableField(string name)
        {
            BaseField field = Fields[name];
            field.guiActive = false;
            field.guiActiveEditor = false;
        }
    }
}
