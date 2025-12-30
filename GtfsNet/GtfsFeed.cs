using System;
using System.Collections.Generic;
using System.Linq;
using GtfsNet.Structs;

namespace GtfsNet
{
    public sealed class GtfsFeed
    {
        public List<Stop> Stops { get; set; }
        public List<StopTime> StopTimes { get; set; }
        public List<Trip> Trips { get; set; }
        public List<Calendar> Calendar { get; set; }
        public List<CalendarDate> CalendarDate { get; set; }
        public List<Route> Route { get; set; }

        private Dictionary<string, List<StopTime>> _stopTimeDictionary;

        public Dictionary<string, List<StopTime>> StopTimesDictionary()
        {
            if (StopTimes is null) return new Dictionary<string, List<StopTime>>();
            if (_stopTimeDictionary is null)
                _stopTimeDictionary = StopTimes
                    .GroupBy(st => st.TripId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(st => st.Sequence).ToList());
            return _stopTimeDictionary;
        }

        // netstandard2.0-friendly RNG
        private static readonly object _rngLock = new object();
        private static readonly Random _rng = new Random();

        private static int NextInt(int maxExclusive)
        {
            lock (_rngLock)
                return _rng.Next(maxExclusive);
        }

        /// <summary>
        /// Returns a random Trip whose route is Tram, Subway, or Train (incl. S-Bahn).
        /// GTFS route_type: 0 = Tram, 1 = Subway, 2 = Train
        /// </summary>
        public Trip GetRandomRailTrip()
        {
            var allowedRouteTypes = new HashSet<int> { 3 };

            var validRouteIds = Route
                .Where(r => allowedRouteTypes.Contains(r.RouteType))
                .Select(r => r.Id).ToList();

            if (validRouteIds.Count == 0)
                throw new InvalidOperationException("No Tram/Subway/Train routes found in GTFS feed.");

            var validTrips = Trips
                .Where(t => validRouteIds.Contains(t.RouteId))
                .ToList();

            if (validTrips.Count == 0)
                throw new InvalidOperationException("No trips found for Tram/Subway/Train routes.");

            return validTrips[NextInt(validTrips.Count)];
        }
    }
}
