using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Contracts;
using FinePrint.Utilities;

namespace SentinelMission
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class SentinelScenario : ScenarioModule
    {
        [KSPField(isPersistant = true)]
        public double NextSpawnTime = double.MinValue;

        private Random generator;

        public void Start()
        {
            if (generator == null)
                generator = new Random();

            // Add a MonoBehaviour to spin the dots on orbital waypoints for Sentinel contracts...
            if (MapView.MapCamera.gameObject.GetComponent<SentinelWaypointManager>() == null)
                MapView.MapCamera.gameObject.AddComponent<SentinelWaypointManager>();

            StartCoroutine(SentinelRoutine());
        }

        public void OnDestroy()
        {
            SentinelWaypointManager dotSpinner = MapView.MapCamera.gameObject.GetComponent<SentinelWaypointManager>();

            if (dotSpinner != null)
                Destroy(dotSpinner);
        }

        private IEnumerator SentinelRoutine()
        {
            while (this)
            {
                double time = Planetarium.GetUniversalTime();

                if (time >= NextSpawnTime)
                {
                    foreach (Vessel v in GetDeployedSentinels())
                    {
                        StartCoroutine(ProcessSentinelScan(v));
                    }

                    // Process one wave of spawns every day "or so".
                    NextSpawnTime = Planetarium.GetUniversalTime() + (KSPUtil.Day*SentinelUtilities.RandomRange(generator, 0.5, 1.5));
                }

                while (Planetarium.GetUniversalTime() < NextSpawnTime)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator ProcessSentinelScan(Vessel v)
        {
            // Each sentinel has a certain chance to detect something every tick.
            if (generator.NextDouble() > SentinelUtilities.SpawnChance)
                yield break;

            // Stagger the spawning of asteroids over a kerbal "work day" so discoveries don't all spawn at once. A real work day is about a third of the day.
            double processTime = Planetarium.GetUniversalTime() + SentinelUtilities.RandomRange(generator, 0, KSPUtil.Day/3.0);

            while (Planetarium.GetUniversalTime() < processTime)
            {
                yield return null;
            }

            CelestialBody innerBody;
            CelestialBody outerBody;
            SentinelUtilities.FindInnerAndOuterBodies(v.orbit.semiMajorAxis, out innerBody, out outerBody);

            Orbit o = SentinelAsteroidOrbit(v.orbit);
            UntrackedObjectClass asteroidClass = SentinelUtilities.WeightedAsteroidClass(generator);

            // The abominable line of doom.
            HighLogic.CurrentGame.AddVessel(ProtoVessel.CreateVesselNode(DiscoverableObjectsUtil.GenerateAsteroidName(), VesselType.SpaceObject, o, 0, new ConfigNode[] { ProtoVessel.CreatePartNode("PotatoRoid", (uint)SentinelUtilities.RandomRange(generator)) }, new ConfigNode("ACTIONGROUPS"), ProtoVessel.CreateDiscoveryNode(DiscoveryLevels.Presence, asteroidClass, SentinelUtilities.RandomRange(generator, KSPUtil.Day * 1, KSPUtil.Day * 20), KSPUtil.Day * 20)));

            if (ContractSystem.Instance != null)
            {
                foreach (SentinelContract c in ContractSystem.Instance.GetCurrentActiveContracts<SentinelContract>())
                {
                    SentinelParameter p = c.GetParameter<SentinelParameter>();

                    if (p == null)
                        continue;

                    p.DiscoverAsteroid(asteroidClass, o.eccentricity, o.inclination, outerBody);
                }
            }
        }

        private List<Vessel> GetDeployedSentinels()
        {
            List<Vessel> sentinels = new List<Vessel>();

            // First find any vessel with the part.
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.loaded)
                {
                    foreach (Part part in v.parts.Where(p => p.name == SentinelUtilities.SentinelPartName))
                    {
                        SentinelModule module = part.FindModuleImplementing<SentinelModule>();

                        if (module != null && module.isTracking)
                        {
                            sentinels.Add(v);
                            break;
                        }
                    }
                }
                else
                {
                    bool hasSentinel = false;

                    foreach (ProtoPartSnapshot part in v.protoVessel.protoPartSnapshots.Where(pps => pps.partName == SentinelUtilities.SentinelPartName))
                    {
                        if (hasSentinel)
                            break;

                        foreach (ProtoPartModuleSnapshot module in part.modules.Where(ppms => ppms.moduleName == SentinelUtilities.SentinelModuleName))
                        {
                            bool tracking = false;
                            SystemUtilities.LoadNode(module.moduleValues, SentinelUtilities.SentinelModuleName, "isTracking", ref tracking, false);

                            if (tracking)
                            {
                                sentinels.Add(v);
                                hasSentinel = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Now remove vessels in that list that are not in proper stable orbits.
            for (int i = sentinels.Count - 1; i >= 0; i--)
            {
                if (!SentinelUtilities.SentinelCanScan(sentinels[i]))
                    sentinels.RemoveAt(i);
            }

            return sentinels;
        }

        private Orbit SentinelAsteroidOrbit(Orbit orbit)
        {
            // Start off by duplicating the orbit of the Sentinel vessel - we need to use the state vectors to make sure the velocity vector is up to date.
            Orbit o = new Orbit(orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis, orbit.LAN, orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, orbit.referenceBody);
            o.UpdateFromStateVectors(orbit.pos, orbit.vel, orbit.referenceBody, Planetarium.GetUniversalTime());

            CelestialBody innerBody;
            CelestialBody outerBody;

            if (SentinelUtilities.FindInnerAndOuterBodies(orbit.semiMajorAxis, out innerBody, out outerBody))
            {
                // Semi-major axis should be deviated a bit from the outer body semi-major axis to make the "band" of detection. It can never be below the semi-major axis of the sentinel though.
                o.semiMajorAxis = Math.Max(o.semiMajorAxis, outerBody.orbit.semiMajorAxis + SentinelUtilities.RandomRange(generator, -((float)outerBody.orbit.semiMajorAxis * 0.1f), ((float)outerBody.orbit.semiMajorAxis * 0.1f)));
            }

            o.Init();
            o.UpdateFromUT(Planetarium.GetUniversalTime());

            // To make these eccentric, we fake a prograde or retrograde "burn", which keeps the position within view of the satellite, while skewing the orbit as a whole.
            Vector3d desiredVelocity;

            if (SystemUtilities.CoinFlip(generator))
            {
                double burnAmount = SentinelUtilities.WeightedRandom(generator) * SentinelUtilities.GetProgradeBurnAllowance(o);
                desiredVelocity = o.vel + o.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * burnAmount;
            }
            else
            {
                double burnAmount = SentinelUtilities.WeightedRandom(generator) * SentinelUtilities.GetRetrogradeBurnAllowance(o);
                desiredVelocity = o.vel + o.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * -burnAmount;
            }

            o.UpdateFromStateVectors(o.pos, desiredVelocity, o.referenceBody, Planetarium.GetUniversalTime());

            // Now rotate it along two axes randomly, with constraints to keep it in a defined +-HalfViewRange cone.
            double viewAngle = SentinelUtilities.AdjustedSentinelViewAngle(orbit, outerBody.orbit) / 2;
            o.meanAnomalyAtEpoch = o.meanAnomalyAtEpoch + (SystemUtilities.CoinFlip(generator) ? SentinelUtilities.WeightedRandom(generator, viewAngle * (Math.PI / 180)) : -SentinelUtilities.WeightedRandom(generator, viewAngle * (Math.PI / 180)));
            o.inclination = o.inclination + (SystemUtilities.CoinFlip(generator) ? SentinelUtilities.WeightedRandom(generator, viewAngle) : -SentinelUtilities.WeightedRandom(generator, viewAngle));

            return o;
        }
    }
}

