using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Contracts;
using FinePrint;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;
using System;

namespace SentinelMission
{
    // For simplicity's sake, we'll use the same configuration options as the science and satellite contracts, using a multiplier to cover difficulty of the request.
    public class SentinelContract : Contract
    {
        System.Random generator;
        CelestialBody innerBody;
        CelestialBody outerBody;
        int asteroidCount = 3;

        protected override bool Generate()
        {
            if (!ProgressUtilities.HavePartTech(SentinelUtilities.SentinelPartName, false))
                return false;

            //Allow a certain amount of accepted contracts, and a separate amount on the board.
            int offeredContracts = 0;
            int activeContracts = 0;
            foreach (SentinelContract contract in ContractSystem.Instance.GetCurrentContracts<SentinelContract>())
            {
                if (contract.ContractState == State.Offered)
                    offeredContracts++;
                else if (contract.ContractState == State.Active)
                    activeContracts++;
            }

            if (offeredContracts >= ContractDefs.Satellite.MaximumAvailable || activeContracts >= ContractDefs.Satellite.MaximumActive)
                return false;

            int superSeed = SystemUtilities.SuperSeed(this);
            generator = new System.Random(superSeed);
            float multiplier = 1;

            SentinelScanType scanType = SentinelScanType.NONE;
            UntrackedObjectClass targetSize = (UntrackedObjectClass)Enum.GetNames(typeof(UntrackedObjectClass)).Length - 1;
            double minimumEccentricity = 0;
            double minimumInclination = 0;
            asteroidCount = generator.Next(5, 11);

            switch (prestige)
            {
                default:
                    SetFocusBody(Planetarium.fetch.Home);
                    break;
                case ContractPrestige.Significant:
                    SetFocusBody(Planetarium.fetch.Home);
                    asteroidCount = generator.Next(10, 16);
                    scanType = SentinelUtilities.RandomScanType(generator);
                    break;
                case ContractPrestige.Exceptional:
                    List<CelestialBody> reachedPlanets = GetBodies_Reached(true, false).Where(cb => cb.referenceBody == Planetarium.fetch.Sun).ToList();

                    // This doesn't ever happen. In case it does happen...
                    if (reachedPlanets.Count <= 0)
                        return false;

                    SetFocusBody(reachedPlanets[generator.Next(0, reachedPlanets.Count)]);
                    asteroidCount = generator.Next(15, 21);
                    scanType = SentinelUtilities.RandomScanType(generator);
                    break;
            }

            switch (scanType)
            {
                case SentinelScanType.CLASS:
                    targetSize = SentinelUtilities.WeightedAsteroidClass(generator);
                    double maxClass = Enum.GetNames(typeof(UntrackedObjectClass)).Length - 1;
                    multiplier = (float)((maxClass - (int)targetSize) / maxClass);
                    multiplier = 0.1f + (multiplier * 0.7f);
                    break;
                case SentinelScanType.ECCENTRICITY:
                    minimumEccentricity = SentinelUtilities.WeightedRandom(generator, SentinelUtilities.MinAsteroidEccentricity, SentinelUtilities.MaxAsteroidEccentricity);
                    multiplier = (float)((minimumEccentricity - SentinelUtilities.MinAsteroidEccentricity) / (SentinelUtilities.MaxAsteroidEccentricity - SentinelUtilities.MinAsteroidEccentricity));
                    multiplier = 0.1f + (multiplier * 0.7f);
                    break;
                case SentinelScanType.INCLINATION:
                    minimumInclination = SentinelUtilities.WeightedRandom(generator, SentinelUtilities.MinAsteroidInclination, SentinelUtilities.MaxAsteroidInclination);
                    multiplier = (float)((minimumInclination - SentinelUtilities.MinAsteroidInclination) / (SentinelUtilities.MaxAsteroidInclination - SentinelUtilities.MinAsteroidInclination));
                    multiplier = 0.1f + (multiplier * 0.7f);
                    break;
                case SentinelScanType.NONE:
                    multiplier = 0.1f;
                    break;
            }

            multiplier *= asteroidCount;

            // Fake out PartRequestParameter.
            ConfigNode requestNode = new ConfigNode("PART_REQUEST");
            requestNode.AddValue("Title", "Have a " + SentinelUtilities.SentinelPartTitle + " on the vessel");
            requestNode.AddValue("Part", SentinelUtilities.SentinelPartName);
            AddParameter(new PartRequestParameter(requestNode));

            // The target orbit should be similar to the outer body's orbit, but have it's SMA adjusted to be slightly larger than the inner body.
            Orbit o = Orbit.CreateRandomOrbitNearby(outerBody.orbit);

            // If the inner body is the sun, the orbit will be null, so we'll use the minimum safe distance.
            if (innerBody == Planetarium.fetch.Sun)
                o.semiMajorAxis = CelestialUtilities.GetMinimumOrbitalAltitude(innerBody, 1) * SentinelUtilities.RandomRange(generator, 1.05, 1.2);
            else
                o.semiMajorAxis = innerBody.orbit.semiMajorAxis * SentinelUtilities.RandomRange(generator, 1.05, 1.2);

            // Now to seal the deal. We use significant deviation, because that is what the scenario uses to start spawning things.
            AddParameter(new SpecificOrbitParameter(OrbitType.RANDOM, o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN, o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, Planetarium.fetch.Sun, ContractDefs.Satellite.SignificantDeviation));

            // And the namesake.
            AddParameter(new SentinelParameter(outerBody, scanType, targetSize, minimumEccentricity, minimumInclination, asteroidCount));

            AddKeywords("Pioneer", "Scientific");

            // Science contracts are the lowest paying contract, we'll use that, the request multiplier, and the asteroid count to determine these.
            SetExpiry(ContractDefs.Research.Expire.MinimumExpireDays, ContractDefs.Research.Expire.MaximumExpireDays);
            SetDeadlineDays(Mathf.RoundToInt(ContractDefs.Research.Expire.DeadlineDays * multiplier), innerBody);
            SetFunds(Mathf.RoundToInt(ContractDefs.Research.Funds.BaseAdvance*multiplier), Mathf.RoundToInt(ContractDefs.Research.Funds.BaseReward * multiplier), Mathf.RoundToInt(ContractDefs.Research.Funds.BaseFailure * multiplier), innerBody);
            SetScience(Mathf.RoundToInt(ContractDefs.Research.Science.BaseReward * multiplier));
            SetReputation(Mathf.RoundToInt(ContractDefs.Research.Reputation.BaseReward * multiplier), Mathf.RoundToInt(ContractDefs.Research.Reputation.BaseFailure * multiplier));

            // Prevent duplicate contracts shortly before finishing up.
            foreach (SentinelContract active in ContractSystem.Instance.GetCurrentContracts<SentinelContract>())
            {
                if (active.outerBody == outerBody)
                    return false;
            }

            return true;
        }

        public override bool CanBeCancelled()
        {
            return true;
        }

        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
            return SystemUtilities.SuperSeed(this).ToString(CultureInfo.InvariantCulture);
        }

        protected override string GetTitle()
        {
            if (outerBody == Planetarium.fetch.Home)
                return "Map " + StringUtilities.IntegerToWord(asteroidCount) + " asteroids endangering " + outerBody.theName + " with a " + SentinelUtilities.SentinelPartTitle + ".";
            else
                return "Map " + StringUtilities.IntegerToWord(asteroidCount) + " asteroids passing near " + StringUtilities.PossessiveString(outerBody.theName) + " orbit with a " + SentinelUtilities.SentinelPartTitle + ".";
        }

        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories(Agent.Name, Agent.GetMindsetString(), "Sentinel", outerBody.name, "asteroids", MissionSeed);
        }

        protected override string GetSynopsys()
        {
            if (outerBody == Planetarium.fetch.Home)
                return "It would be much easier to track asteroids in " + StringUtilities.PossessiveString(outerBody.theName) + " blind spots from afar with a " + SentinelUtilities.SentinelPartTitle + " deployed in solar orbit near " + innerBody.theName + ".";
            else
                return "The " + SentinelUtilities.SentinelPartTitle + " has worked so well mapping " + StringUtilities.PossessiveString(Planetarium.fetch.Home.theName) + " orbit, that we would like you to use one to map asteroids around " + outerBody.theName + ".";
        }

        protected override string MessageCompleted()
        {
            return "You have successfully mapped a portion of the asteroids that pass near " + StringUtilities.PossessiveString(outerBody.theName) + " orbit.";
        }

        protected override void OnLoad(ConfigNode node)
        {
            SystemUtilities.LoadNode(node, "SentinelContract", "innerBody", ref innerBody, Planetarium.fetch.Sun);
            SystemUtilities.LoadNode(node, "SentinelContract", "outerBody", ref outerBody, Planetarium.fetch.Home);
            SystemUtilities.LoadNode(node, "SentinelContract", "asteroidCount", ref asteroidCount, 3);
        }

        protected override void OnSave(ConfigNode node)
        {
            int innerBodyID = innerBody.flightGlobalsIndex;
            node.AddValue("innerBody", innerBodyID);

            int outerBodyID = outerBody.flightGlobalsIndex;
            node.AddValue("outerBody", outerBodyID);

            node.AddValue("asteroidCount", asteroidCount);
        }

        public override bool MeetRequirements()
        {
            // We need to make sure the player can see orbits, that the player has orbited the sun before, and that the player has the Sentinel part unlocked.
            if (!ProgressTracking.Instance.NodeComplete(Planetarium.fetch.Sun.name, "Orbit"))
                return false;

            if (GameVariables.Instance.GetOrbitDisplayMode(ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation)) < GameVariables.OrbitDisplayMode.AllOrbits)
                return false;

            return true;
        }

        public void SetFocusBody(CelestialBody body)
        {
            if (!SentinelUtilities.IsOnSolarOrbit(body))
            {
                innerBody = Planetarium.fetch.Sun;
                outerBody = Planetarium.fetch.Home;
            }
            else
                SentinelUtilities.FindInnerAndOuterBodies(body.orbit.semiMajorAxis - 1, out innerBody, out outerBody);
        }
    }
}
