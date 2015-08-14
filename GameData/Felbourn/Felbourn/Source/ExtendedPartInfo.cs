//#define USE_KSP_API

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TweakScale;
using RemoteTech.Modules;

#if USE_KSP_API
    using KSPAPIExtensions;
    using KSPAPIExtensions.PartMessage;
#endif

namespace ExtendedPartInfo
{
    public class ExtendedPartInfo : PartModule, TweakScale.IRescalable<ExtendedPartInfo>
    {
        KSPAssembly remoteTech = new KSPAssembly("RemoteTech", 1, 6);

        // all

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Part")]
        private string PartName = "";
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        private string Internal = "";
        [KSPField(isPersistant = false, guiActiveEditor = true)]
        private string Docking = null;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiFormat = "N0", guiUnits = " kg")]
        private float Mass = 0;
        [KSPField(isPersistant = false)]
        public float OriginalMass = 0;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private bool Shielded = false;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        //private string Temp = "";
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        //private string SkinTemp = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private string Flux = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private bool CrossFeed = false;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true)]
        private string Scale = "";

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
        private ModuleDockingNode myDocking = null;

        private Part defaultPart = new Part();

        //-----------------------------------------------------------------------------------------
        // interface
        public override void OnAwake()
        {
            //Debug.Log("[EPI] OnAwake " + part.partInfo.name);
            base.OnAwake();
            #if USE_KSP_API
                PartMessageService.Register(this);
            #endif
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            //Debug.Log("[EPI] start " + part.partInfo.name);

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

                if ((part.Modules[i] as ModuleDockingNode) != null)
                    myDocking = part.Modules[i] as ModuleDockingNode;
            }

            Display();

            if (myDocking != null)
                Docking = myDocking.nodeType;
            else
                DisableField("Docking");

            PartName = part.partInfo.name;
            if (part.CrewCapacity > 0)
                Internal = part.CrewCapacity + "x ";
                var node = part.partInfo.internalConfig.GetNode("INTERNAL");
                if (node != null)
                    Internal += node.GetValue("name");
            else
                DisableField("Internal");

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
            Debug.Log("[EPI] TweakScale change");
            StartCoroutine(DelayDisplay());
        }
        private System.Collections.IEnumerator DelayDisplay()
        {
            yield return null;
            Display();
        }

        #if USE_KSP_API
            [PartMessageListener(typeof(PartMassChanged), scenes:GameSceneFilter.AnyEditor)]
            public void PartMassChanged(float mass)
            {
                Debug.Log("[EPI] PartMassChanged");
                Mass = mass * 1000;
            }
        #endif

        private void Display()
        {
            try
            {
                Mass = (part.mass + part.GetResourceMass()) * 1000;
                CrossFeed = part.fuelCrossFeed;

                if (part.scaleFactor != 1 || part.rescaleFactor != 1)
                    Scale = part.scaleFactor + " / " + part.rescaleFactor;
                else
                    DisableField("Scale");

                //Temp = Math.Round(part.temperature) + " / " + part.maxTemp;
                //SkinTemp = Math.Round(part.skinTemperature) + " / " + part.skinMaxTemp;

                Flux = "";
                bool comma = true;
                comma = ShowFlux("s", part.skinToInternalFlux, comma);
                comma = ShowFlux("c", part.thermalConductionFlux, comma);
                comma = ShowFlux("v", part.thermalConvectionFlux, comma);
                comma = ShowFlux("i", part.thermalInternalFlux, comma);
                comma = ShowFlux("r", part.thermalRadiationFlux, comma);
                if (Flux == "")
                    DisableField("Flux");

                Shielded = part.ShieldedFromAirstream;

                if (myRCS)
                    Thrust = myRCS.thrusterPower;
                else if (myEngine)
                    Thrust = myEngine.maxThrust;
                else
                    DisableField("Thrust");

                if (CrashTolerance != defaultPart.crashTolerance)
                    CrashTolerance = part.crashTolerance;
                else
                    DisableField("CrashTolerance");

                if (BreakForce != defaultPart.breakingForce)
                    BreakForce = part.breakingForce;
                else
                    DisableField("BreakForce");

                if (BreakTorque != defaultPart.breakingTorque)
                    BreakTorque = part.breakingTorque;
                else
                    DisableField("BreakTorque");

                if (myCMG)
                    GyroTorque = myCMG.PitchTorque;
                else
                    DisableField("GyroTorque");
            }
            catch (Exception e)
            {
                Debug.LogError("[EPI] Display exception " + e.InnerException);
                Debug.Log(e.StackTrace);
            }
        }

        //-----------------------------------------------------------------------------------------
        private bool ShowFlux(string units, double value, bool comma)
        {
            if (Math.Abs(value) < 100)
                return comma;
            Flux += units + IntWithUnits((float)value);
            if (comma)
                Flux += ",";
            return false;
        }

        //-----------------------------------------------------------------------------------------
        public override string GetInfo()
        {
            try
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

                    //Debug.Log("[EPI] omni = " + antenna.Mode1OmniRange);
                    //Debug.Log("[EPI] dish = " + antenna.Mode1DishRange);
                    //Debug.Log("[EPI] range = " + range);

                    remoteTech += "  75km: " + RemoteTech(range, KM(75), maxRange) + "\n";
                    remoteTech += " 250km: " + RemoteTech(range, KM(250), maxRange) + "\n";
                    remoteTech += "   1Mm: " + RemoteTech(range, MM(1), maxRange) + "\n";
                    remoteTech += " 3.5Mm: " + RemoteTech(range, MM(3.5f), maxRange) + "\n";
                    remoteTech += "   8Mm: " + RemoteTech(range, MM(8), maxRange) + "\n";
                    remoteTech += "  20Mm: " + RemoteTech(range, MM(20), maxRange) + "\n";
                    remoteTech += "  45Mm: " + RemoteTech(range, MM(45), maxRange) + "\n";
                    remoteTech += "   8Gm: " + RemoteTech(range, GM(8), maxRange) + "\n";
                    remoteTech += "  15Gm: " + RemoteTech(range, GM(15), maxRange) + "\n";
                    remoteTech += "  32Gm: " + RemoteTech(range, GM(32), maxRange) + "\n";
                    remoteTech += "  93Gm: " + RemoteTech(range, GM(93), maxRange) + "\n";
                }

                string stats =
                    ((part.PhysicsSignificance != defaultPart.PhysicsSignificance) ? "PhysicsSignificance " + part.PhysicsSignificance + "\n" : "") +
                    ((part.explosionPotential != defaultPart.explosionPotential) ? "explosionPotential " + part.explosionPotential + " (" + defaultPart.explosionPotential + ")\n" : "") +
                    ((part.emissiveConstant != defaultPart.emissiveConstant) ? "emissiveConstant " + part.emissiveConstant + " (" + defaultPart.emissiveConstant + ")\n" : "") +
                    ((part.heatConductivity != defaultPart.heatConductivity) ? "heatConductivity " + part.heatConductivity + " (" + defaultPart.heatConductivity + ")\n" : "") +
                    ((part.heatConvectiveConstant != defaultPart.heatConvectiveConstant) ? "heatConvectiveConstant " + part.heatConvectiveConstant + " (" + defaultPart.heatConvectiveConstant + ")\n" : "") +
                    ((part.radiativeArea != defaultPart.radiativeArea) ? "radiativeArea " + part.radiativeArea + " (" + defaultPart.radiativeArea + ")\n" : "") +
                    ((part.radiatorHeadroom != defaultPart.radiatorHeadroom) ? "radiatorHeadroom " + part.radiatorHeadroom + " (" + defaultPart.radiatorHeadroom + ")\n" : "") +
                    ((part.radiatorMax != defaultPart.radiatorMax) ? "radiatorMax " + part.radiatorMax + " (" + defaultPart.radiatorMax + ")\n" : "") +
                    ((part.resourceThermalMass != defaultPart.resourceThermalMass) ? "resourceThermalMass " + part.resourceThermalMass + " (" + defaultPart.resourceThermalMass + ")\n" : "") +
                    ((part.thermalMass != defaultPart.thermalMass) ? "thermalMass " + part.thermalMass + " (" + defaultPart.thermalMass + ")\n" : "") +
                    ((part.thermalMassModifier != defaultPart.thermalMassModifier) ? "thermalMassModifier " + part.thermalMassModifier + " (" + defaultPart.thermalMassModifier + ")\n" : "")
                ;
                return partName + path + massInfo + remoteTech + stats;
            }
            catch (Exception e)
            {
                Debug.LogError("[EPI] Display exception " + e.InnerException);
                Debug.Log(e.StackTrace);
                return e.InnerException.ToString();
            }
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
