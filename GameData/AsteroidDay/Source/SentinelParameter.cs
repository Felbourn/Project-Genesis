using System;
using System.Globalization;
using Contracts;
using FinePrint.Utilities;

namespace SentinelMission
{
    public class SentinelParameter : ContractParameter
    {
        public CelestialBody FocusBody;
        public int TotalDiscoveries;
        public int RemainingDiscoveries;
        public SentinelScanType ScanType;
        public UntrackedObjectClass TargetSize;
        public double MinimumEccentricity;
        public double MinimumInclination;

        public SentinelParameter()
        {
            FocusBody = Planetarium.fetch.Home;
            ScanType = SentinelScanType.NONE;
            TargetSize = (UntrackedObjectClass)Enum.GetNames(typeof(UntrackedObjectClass)).Length - 1;
            MinimumEccentricity = 0;
            MinimumInclination = 0;
            TotalDiscoveries = 3;
            RemainingDiscoveries = 3;
        }

        public SentinelParameter(CelestialBody focusBody, SentinelScanType scanType, UntrackedObjectClass targetSize, double minimumEccentricity, double minimumInclination, int discoveryCount)
        {
            FocusBody = focusBody;
            ScanType = scanType;
            TargetSize = targetSize;
            MinimumEccentricity = minimumEccentricity;
            MinimumInclination = minimumInclination;
            TotalDiscoveries = discoveryCount;
            RemainingDiscoveries = discoveryCount;
        }

        protected override string GetHashString()
        {
            return SystemUtilities.SuperSeed(Root).ToString(CultureInfo.InvariantCulture) + ID;
        }

        protected override string GetTitle()
        {
            string positionString = (FocusBody == Planetarium.fetch.Home ? "threatening " : "around ");
            switch (ScanType)
            {
                case SentinelScanType.CLASS:
                    return "Map " + StringUtilities.IntegerToWord(TotalDiscoveries) + " class " + TargetSize + " asteroids " + positionString + FocusBody.theName;
                case SentinelScanType.ECCENTRICITY:
                    return "Map " + StringUtilities.IntegerToWord(TotalDiscoveries) + " asteroids " + positionString + FocusBody.theName + " with an eccentricity greater than " + Math.Round(MinimumEccentricity, 2);
                case SentinelScanType.INCLINATION:
                    return "Map " + StringUtilities.IntegerToWord(TotalDiscoveries) + " asteroids " + positionString + FocusBody.theName + " with an inclination greater than " + Math.Round(MinimumInclination);
                default:
                    return "Map " + StringUtilities.IntegerToWord(TotalDiscoveries) + " asteroids " + positionString + FocusBody.theName;
            }
        }

        protected override string GetNotes()
        {
            string s = HighLogic.LoadedScene == GameScenes.SPACECENTER ? "\n" : "";
            s += "The mapping process will happen passively over a length of time as long as any active sentinels are near the specified orbit. They do not need to be newly launched. You will receive progress notifications as suitable asteroids are mapped.";
            return s;
        }

        protected override void OnRegister()
        {
            DisableOnStateChange = true;
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("FocusBody", FocusBody.flightGlobalsIndex);
            node.AddValue("ScanType", ScanType);
            node.AddValue("TargetSize", TargetSize);
            node.AddValue("MinimumEccentricity", MinimumEccentricity);
            node.AddValue("MinimumInclination", MinimumInclination);
            node.AddValue("TotalDiscoveries", TotalDiscoveries);
            node.AddValue("RemainingDiscoveries", RemainingDiscoveries);
        }

        protected override void OnLoad(ConfigNode node)
        {
            SystemUtilities.LoadNode(node, "SentinelParameter", "FocusBody", ref FocusBody, Planetarium.fetch.Home);
            SystemUtilities.LoadNode(node, "SentinelParameter", "ScanType", ref ScanType, SentinelScanType.NONE);
            SystemUtilities.LoadNode(node, "SentinelParameter", "TargetSize", ref TargetSize, (UntrackedObjectClass)Enum.GetNames(typeof(UntrackedObjectClass)).Length - 1);
            SystemUtilities.LoadNode(node, "SentinelParameter", "MinimumEccentricity", ref MinimumEccentricity, 0);
            SystemUtilities.LoadNode(node, "SentinelParameter", "MinimumInclination", ref MinimumInclination, 0);
            SystemUtilities.LoadNode(node, "SentinelParameter", "TotalDiscoveries", ref TotalDiscoveries, 3);
            SystemUtilities.LoadNode(node, "SentinelParameter", "RemainingDiscoveries", ref RemainingDiscoveries, 3);
        }

        public void DiscoverAsteroid(UntrackedObjectClass size, double eccentricity, double inclination, CelestialBody body)
        {
            if (Root.ContractState != Contract.State.Active)
                return;

            if (body != FocusBody)
                return;

            if (ScanType == SentinelScanType.CLASS && size != TargetSize)
                return;

            if (ScanType == SentinelScanType.ECCENTRICITY && eccentricity < MinimumEccentricity)
                return;

            if (ScanType == SentinelScanType.INCLINATION && inclination < MinimumInclination)
                return;

            RemainingDiscoveries--;

            if (RemainingDiscoveries > 0)
            {
                string s = "A sentinel has now mapped " + (TotalDiscoveries - RemainingDiscoveries) + "/" + TotalDiscoveries + " suitable asteroids near " + body.theName + " for " + Root.Agent.Name + ".";
                ScreenMessages.PostScreenMessage(s, SentinelUtilities.CalculateReadDuration(s), ScreenMessageStyle.UPPER_LEFT);
            }
            else
            {
                string s = "Sentinels have finished mapping suitable asteroids around " + FocusBody.theName + " for " + Root.Agent.Name + ".";
                ScreenMessages.PostScreenMessage(s, SentinelUtilities.CalculateReadDuration(s), ScreenMessageStyle.UPPER_CENTER);
                SetComplete();
                Root.Complete();
            }
        }
    }
}
