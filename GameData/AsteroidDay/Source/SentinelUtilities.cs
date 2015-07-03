using System;
using System.Collections.Generic;
using System.Linq;
using FinePrint;
using FinePrint.Utilities;

namespace SentinelMission
{
    public enum SentinelScanType { NONE, CLASS, ECCENTRICITY, INCLINATION };

    public static class SentinelUtilities
    {
        // Open these for tinkering.
        public static string SentinelPartName { get; set; } = "InfraredTelescope";
        public static string SentinelModuleName { get; set; } = "SentinelModule";
        public static double SentinelViewAngle { get; set; } = 200;
        public static float SpawnChance { get; set; } = 0.25f;
        public static int WeightedStability { get; set; } = 3;
        public static double MinAsteroidEccentricity = 0.05;
        public static double MaxAsteroidEccentricity = 0.4;
        public static double MinAsteroidInclination = 5;
        public static double MaxAsteroidInclination = 40;

        public static string SentinelPartTitle
        {
            get
            {
                if (sentinelPartTitle != "")
                    return sentinelPartTitle;

                AvailablePart sentinelPart = PartLoader.getPartInfoByName(SentinelPartName);
                return sentinelPart == null ? "SENTINEL Infrared Telescope" : sentinelPart.title;
            }
        }

        private static string sentinelPartTitle = "";

        /// <summary>
        /// Determines if a vessel is properly aligned for sentinel scanning.
        /// </summary>
        /// <param name="v">The vessel.</param>
        /// <param name="innerBody">Optional inner body.</param>
        /// <param name="outerBody">Optional outer body.</param>
        /// <returns>If the vessel is properly aligned for sentinel scanning.</returns>
        public static bool SentinelCanScan(Vessel v, CelestialBody innerBody = null, CelestialBody outerBody = null)
        {
            double vesselInclination;
            double vesselEccentricity;

            // We need vessels that are orbiting the sun, between two planets of similar inclinations, at an inclination similar to them.
            if (v.loaded)
            {
                if (v.situation != Vessel.Situations.ORBITING || v.orbit.referenceBody != Planetarium.fetch.Sun)
                    return false;

                vesselInclination = v.orbit.inclination;
                vesselEccentricity = v.orbit.eccentricity;
            }
            else
            {
                if (v.protoVessel.situation != Vessel.Situations.ORBITING || FlightGlobals.Bodies[v.protoVessel.orbitSnapShot.ReferenceBodyIndex] != Planetarium.fetch.Sun)
                    return false;

                vesselInclination = v.protoVessel.orbitSnapShot.inclination;
                vesselEccentricity = v.protoVessel.orbitSnapShot.eccentricity;
            }

            if (innerBody == null || outerBody == null)
            {
                if (!FindInnerAndOuterBodies(v, out innerBody, out outerBody))
                    return false;
            }

            // Do a basic check to make sure the eccentricities are similar.
            if (Math.Abs(outerBody.orbit.eccentricity - vesselEccentricity) > 0.2f)
                return false;

            // The vessel needs to be at a similar orbit as the outer planet or mapping it would be tough we'll use the satellite contract mid-level deviations for angle comparison.
            if (Math.Abs(Math.Abs(outerBody.orbit.inclination) - Math.Abs(vesselInclination)) > (ContractDefs.Satellite.SignificantDeviation / 100) * 90)
                return false;

            // Unless the orbit is horizontal, we check the LAN as well.
            if (Math.Abs(outerBody.orbit.inclination) % 180 >= 1)
            {
                double vesselLAN = v.orbit.LAN;
                double bodyLAN = outerBody.orbit.LAN;

                if (v.orbit.inclination < 0)
                    vesselLAN = (vesselLAN + 180) % 360;

                if (outerBody.orbit.inclination < 0)
                    bodyLAN = (bodyLAN + 180) % 360;

                float lanDifference = (float)Math.Abs(vesselLAN - bodyLAN) % 360;

                if (lanDifference > 180)
                    lanDifference = 360 - lanDifference;

                if (lanDifference > ((ContractDefs.Satellite.SignificantDeviation / 100) * 360.0))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Uses an arbitrary semi-major axis to determine the bodies on either side of it.
        /// </summary>
        /// <param name="SMA">The semi-major axis.</param>
        /// <param name="innerBody">The body to store the inner body in.</param>
        /// <param name="outerBody">The body to store the outer body in.</param>
        /// <returns>False if the semi-major axis falls in a weird spot.</returns>
        public static bool FindInnerAndOuterBodies(double SMA, out CelestialBody innerBody, out CelestialBody outerBody)
        {
            // Find the two bodies this Sentinel is between simply by comparing semi major axes. Find together to prevent doing this twice for no reason.
            Dictionary<double, CelestialBody> smaMap = new Dictionary<double, CelestialBody>();

            foreach (CelestialBody cb in FlightGlobals.Bodies)
            {
                if (cb == Planetarium.fetch.Sun)
                    smaMap.Add(0, cb);
                else if (cb.referenceBody == Planetarium.fetch.Sun)
                    smaMap.Add(cb.orbit.semiMajorAxis, cb);
            }

            List<double> sortedMap = smaMap.Keys.ToList();
            sortedMap.Sort();

            for (int i = 0; i < sortedMap.Count; i++)
            {
                if (sortedMap[i] <= SMA && sortedMap[i + 1] > SMA)
                {
                    innerBody = smaMap[sortedMap[i]];
                    outerBody = smaMap[sortedMap[i + 1]];

                    return true;
                }
            }

            // If it isn't between two actual planets, we'll just default to the sun and the outer planet. *Should* never happen.
            innerBody = smaMap[sortedMap[0]];
            outerBody = smaMap[sortedMap[sortedMap.Count - 1]];

            // Return false if it does happen.
            return false;
        }

        /// <summary>
        /// Uses semi-major axis of an orbit to determine the bodies on either side of it.
        /// </summary>
        /// <param name="o">The orbit.</param>
        /// <param name="innerBody">The body to store the inner body in.</param>
        /// <param name="outerBody">The body to store the outer body in.</param>
        /// <returns>False if the semi-major axis falls in a weird spot.</returns>
        public static bool FindInnerAndOuterBodies(Orbit o, out CelestialBody innerBody, out CelestialBody outerBody)
        {
            double sma = o.semiMajorAxis;

            if (o.referenceBody != null && o.referenceBody != Planetarium.fetch.Sun)
            {
                CelestialBody currentPlanet = o.referenceBody;

                while (currentPlanet != Planetarium.fetch.Sun && currentPlanet.referenceBody != null && currentPlanet.referenceBody != Planetarium.fetch.Sun)
                {
                    currentPlanet = currentPlanet.orbit.referenceBody;
                }

                sma = currentPlanet.orbit.semiMajorAxis;
            }

            return FindInnerAndOuterBodies(sma, out innerBody, out outerBody);
        }

        /// <summary>
        /// Uses semi-major axis of a vessel's orbit to determine the bodies on either side of it.
        /// </summary>
        /// <param name="v">The vessel.</param>
        /// <param name="innerBody">The body to store the inner body in.</param>
        /// <param name="outerBody">The body to store the outer body in.</param>
        /// <returns>False if the semi-major axis falls in a weird spot.</returns>
        public static bool FindInnerAndOuterBodies(Vessel v, out CelestialBody innerBody, out CelestialBody outerBody)
        {
            double sma;
            CelestialBody referenceBody;

            if (v.loaded)
            {
                sma = v.orbit.semiMajorAxis;
                referenceBody = v.orbit.referenceBody;
            }
            else
            {
                sma = v.protoVessel.orbitSnapShot.semiMajorAxis;
                referenceBody = FlightGlobals.Bodies[v.protoVessel.orbitSnapShot.ReferenceBodyIndex];
            }

            if (referenceBody != Planetarium.fetch.Sun)
            {
                CelestialBody currentPlanet = referenceBody;

                while (currentPlanet != Planetarium.fetch.Sun && currentPlanet.referenceBody != null && currentPlanet.referenceBody != Planetarium.fetch.Sun)
                {
                    currentPlanet = currentPlanet.orbit.referenceBody;
                }

                sma = currentPlanet.orbit.semiMajorAxis;
            }

            return FindInnerAndOuterBodies(sma, out innerBody, out outerBody);
        }

        /// <summary>
        /// Calculates the viewing angle offset created by a sentinel's distance from its focus body.
        /// </summary>
        /// <param name="innerOrbit">The sentinel's orbit.</param>
        /// <param name="outerOrbit">The focus body's orbit.</param>
        /// <returns>An offset viewing angle.</returns>
        public static double AdjustedSentinelViewAngle(Orbit innerOrbit, Orbit outerOrbit)
        {
            double innerRadius = innerOrbit.semiMajorAxis / 2;
            double outerRadius = outerOrbit.semiMajorAxis / 2;
            double a = (Math.PI / 180) * ((360 - SentinelViewAngle) / 2);
            double b1 = Math.Asin((innerRadius / outerRadius) * Math.Sin(a));
            double b2 = Math.PI - b1;
            double c1 = (Math.PI - a - b1) * (360 / (Math.PI * 2));
            double c2 = (Math.PI - a - b2) * (360 / (Math.PI * 2));

            double viewAngle = Math.Max(c1, c2) * 2;

            // These clamps are not entirely necessary, but it never hurts.
            if (viewAngle > SentinelViewAngle)
                viewAngle = SentinelViewAngle;

            if (viewAngle < SentinelViewAngle / 100)
                viewAngle = SentinelViewAngle / 100;

            return viewAngle;
        }

        /// <summary>
        /// Calculates the velocity required to escape a body at a certain altitude.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="altitude">The altitude.</param>
        /// <returns>The required velocity to escape the body.</returns>
        public static double GetEscapeVelocity(CelestialBody body, double altitude)
        {
            if (body != Planetarium.fetch.Sun)
            {
                if (altitude <= body.sphereOfInfluence)
                    return Math.Sqrt(((2 * body.gravParameter) / altitude) - (body.gravParameter / (body.Radius + body.sphereOfInfluence)));
                else
                    return 0;
            }

            return  Math.Sqrt((2 * body.gravParameter) / altitude);
        }

        /// <summary>
        /// Calculates the slowest velocity to possibly maintain while still in orbit of a body.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <returns>The slowest velocity you can orbit the body at.</returns>
        public static double GetMinimumOrbitalSpeed(CelestialBody body)
        {
            double soi = body == Planetarium.fetch.Sun ? CelestialUtilities.GetSolarExtents() : body.sphereOfInfluence;
            return Math.Sqrt(body.gravParameter / soi);
        }

        /// <summary>
        /// Calculates how much velocity a sentinel spawned asteroid has to work with in a prograde eccentricity burn.
        /// </summary>
        /// <param name="o">The asteroid's orbit.</param>
        /// <returns>The amount the asteroid can "burn" without escaping the sun.</returns>
        public static double GetProgradeBurnAllowance(Orbit o)
        {
            return GetEscapeVelocity(o.referenceBody, o.altitude + o.referenceBody.Radius) - o.GetRelativeVel().magnitude;
        }

        /// <summary>
        /// Calculates how much velocity a sentinel spawned asteroid has to work with in a retrograde eccentricity burn.
        /// </summary>
        /// <param name="o">The asteroid's orbit.</param>
        /// <returns>The amount the asteroid can "burn" without diving into the sun.</returns>
        public static double GetRetrogradeBurnAllowance(Orbit o)
        {
            return o.GetRelativeVel().magnitude - GetMinimumOrbitalSpeed(o.referenceBody);
        }

        /// <summary>
        /// Gets the discovery object class of a vessel, loaded or not.
        /// </summary>
        /// <param name="v">The vessel.</param>
        /// <returns>The vessel's discovery class.</returns>
        public static UntrackedObjectClass GetVesselClass(Vessel v)
        {
            UntrackedObjectClass size = UntrackedObjectClass.C;

            if (!v.loaded)
            {
                if (v.protoVessel.discoveryInfo.HasValue("size"))
                    size = (UntrackedObjectClass)int.Parse(v.protoVessel.discoveryInfo.GetValue("size"));
            }
            else
                size = v.DiscoveryInfo.objectSize;

            return size;
        }

        /// <summary>
        /// Picks a random scan type.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <returns>A random scan type.</returns>
        public static SentinelScanType RandomScanType(System.Random generator = null)
        {
            if (generator == null)
                generator = new System.Random();

            return (SentinelScanType)generator.Next(0, Enum.GetNames(typeof(SentinelScanType)).Length);
        }

        /// <summary>
        /// Chooses a random asteroid class, weighted towards larger classes.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <returns>A random weighted asteroid class.</returns>
        public static UntrackedObjectClass WeightedAsteroidClass(System.Random generator = null)
        {
            if (generator == null)
                generator = new System.Random();

            // Larger asteroids are easier to see.
            int size = Enum.GetNames(typeof(UntrackedObjectClass)).Length - 1;

            while (size > 0 && generator.Next(100) > 50)
            {
                size--;
            }

            return (UntrackedObjectClass)size;
        }

        /// <summary>
        /// Chooses a random number from 0-1, weighted towards zero by WeightedStability.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <returns>A random number from 0-1.</returns>
        public static double WeightedRandom(System.Random generator = null)
        {
            if (generator == null)
                generator = new System.Random();

            return Math.Pow(generator.NextDouble(), WeightedStability);
        }

        /// <summary>
        /// Chooses a random number from min-max, weighted towards min by WeightedStability.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <param name="min">The minimum generated value.</param>
        /// <param name="max">The maximum generated value.</param>
        /// <returns>A random number from min-max.</returns>
        public static double WeightedRandom(System.Random generator = null, double min = 0, double max = 100)
        {
            if (generator == null)
                generator = new System.Random();

            return min + (Math.Pow(generator.NextDouble(), WeightedStability) * (max - min));
        }

        /// <summary>
        /// Chooses a random number from 0-max, weighted towards 0 by WeightedStability.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <param name="max">The maximum generated value.</param>
        /// <returns>A random number from 0-max.</returns>
        public static double WeightedRandom(System.Random generator = null, double max = 100)
        {
            if (generator == null)
                generator = new System.Random();

            return WeightedRandom(generator, 0, max);
        }

        /// <summary>
        /// Chooses a random unweighted number from min-max.
        /// </summary>
        /// <param name="generator">An optional random generator.</param>
        /// <param name="min">The minimum generated value.</param>
        /// <param name="max">The maximum generated value.</param>
        /// <returns></returns>
        public static double RandomRange(System.Random generator = null, double min = double.MinValue, double max = double.MaxValue)
        {
            if (generator == null)
                generator = new System.Random();

            double a = min;
            double b = max;
            min = Math.Min(a, b);
            max = Math.Max(a, b);

            // Not going to lie - this function exists because I dislike UnityEngine.Random.
            return min + (generator.NextDouble() * (max - min));
        }

        /// <summary>
        /// Calculates how long a message of any length should display using average reading speeds.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The average time (in seconds) it takes to read the string.</returns>
        public static float CalculateReadDuration(string s)
        {
            float averageWordsPerMinute = 200;
            float averageWordLength = 5;
            float wordCount = s.Length/averageWordLength;
            return (wordCount / averageWordsPerMinute) * 60;
        }

        /// <summary>
        /// Determines if a body is directly orbiting the sun.
        /// </summary>
        /// <param name="body">The CelestialBody.</param>
        /// <returns>If the CelestialBody is orbiting the sun.</returns>
        public static bool IsOnSolarOrbit(CelestialBody body)
        {
            if (body == Planetarium.fetch.Sun || body.referenceBody == null || body.referenceBody != Planetarium.fetch.Sun)
                return false;

            return true;
        }
    }
}
