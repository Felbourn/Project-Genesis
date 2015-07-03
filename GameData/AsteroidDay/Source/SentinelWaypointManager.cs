using UnityEngine;
using Contracts;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;

namespace SentinelMission
{
    class SentinelWaypointManager : MonoBehaviour
    {
        public void OnPreCull()
        {
            if (MapView.MapIsEnabled)
            {
                if (ContractSystem.Instance == null)
                    return;

                CelestialBody mapFocus = CelestialUtilities.MapFocusBody();

                foreach (SentinelContract c in ContractSystem.Instance.GetCurrentContracts<SentinelContract>())
                {
                    SpecificOrbitParameter p = c.GetParameter<SpecificOrbitParameter>();

                    if (p == null)
                        continue;

                    bool focused = mapFocus != null && mapFocus.GetName() == p.targetBody.GetName();
                    p.updateMapIcons(focused);
                }
            }
        }
    }
}
