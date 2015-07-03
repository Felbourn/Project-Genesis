using FinePrint;
using FinePrint.Utilities;

namespace SentinelMission
{
    public class SentinelModule : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool isTracking = false;

        [KSPEvent(guiName = "Start Object Tracking", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StartTracking()
        {
            if (!TelescopeCanActivate())
                return;

            ShowMessage("The " + SentinelUtilities.SentinelPartTitle + " is now mapping asteroids passing near " + StringUtilities.PossessiveString(FocusName) + " orbit.");
            isTracking = true;
            Events["StartTracking"].active = false;
            Events["StopTracking"].active = true;
            MonoUtilities.RefreshContextWindows(part);
        }

        [KSPEvent(guiName = "Stop Object Tracking", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = false, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void StopTracking()
        {
            ShowMessage("The " + SentinelUtilities.SentinelPartTitle + " is no longer mapping asteroids passing near " + StringUtilities.PossessiveString(FocusName) + " orbit.");
            isTracking = false;
            Events["StartTracking"].active = true;
            Events["StopTracking"].active = false;
            MonoUtilities.RefreshContextWindows(part);
        }

        [KSPField(guiActive = true, guiName = "Telescope", guiUnits = "", guiFormat = "F3")]
        public string status = "Off";

        public void FixedUpdate()
        {
            if (isTracking)
            {
                CelestialBody innerBody;
                CelestialBody outerBody;

                // Get planets ahead of time to avoid having to do it more than once.
                if (SentinelUtilities.FindInnerAndOuterBodies(vessel, out innerBody, out outerBody))
                    status = SentinelUtilities.SentinelCanScan(vessel, innerBody, outerBody) ? "Mapping " + outerBody.theName : "Misaligned with " + outerBody.theName;
                else
                    status = "Error";
            }
            else
                status = "Inactive";
        }

        private string FocusName
        {
            get
            {
                CelestialBody innerBody;
                CelestialBody outerBody;

                SentinelUtilities.FindInnerAndOuterBodies(vessel, out innerBody, out outerBody);

                return outerBody.theName;
            }
        }

        private bool TelescopeCanActivate()
        {
            bool hasAntenna = VesselUtilities.ShipHasPartsOrModules(null, ContractDefs.GetModules("Antenna"));
            bool hasPower = VesselUtilities.ShipHasPartsOrModules(null, ContractDefs.GetModules("Power"));

            if (!hasPower)
            {
                ShowMessage("The " + SentinelUtilities.SentinelPartTitle + " cannot activate without a power generator to provide electric charge.");
                return false;
            }

            if (!hasAntenna)
            {
                ShowMessage("The " + SentinelUtilities.SentinelPartTitle + " cannot activate without an antenna present to transmit the data it gathers.");
                return false;
            }

            if (vessel.orbit.referenceBody != Planetarium.fetch.Sun)
            {
                ShowMessage("The " + SentinelUtilities.SentinelPartTitle + " needs to be activated on a solar orbit.");
                return false;
            }

            return true;
        }

        private void ShowMessage(string s)
        {
            ScreenMessages.PostScreenMessage(s, SentinelUtilities.CalculateReadDuration(s), ScreenMessageStyle.UPPER_CENTER);
        }
    }
}
