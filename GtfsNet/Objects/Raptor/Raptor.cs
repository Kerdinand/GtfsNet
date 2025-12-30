using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CsvHelper.Configuration;
using GtfsNet.Helper.TimeTable;

namespace GtfsNet.Objects.Raptor
{
    public class Raptor
    {
        private Dictionary<string, TimeTable> _timeTables;
        private List<TimeTableTrip> _timeTableTrips;

        public Raptor(Dictionary<string, TimeTable> timeTables, List<TimeTableTrip> timeTableTrips)
        {
            _timeTables = timeTables;
            _timeTableTrips = timeTableTrips;
        }

        public string GetNextDepartures(string stopId, DateTime time)
        {
            var timeTables = new List<TimeTable>();

            var tt = _timeTables[stopId];

            // walk to root stop
            var root = tt;
            while (root.Parent != null)
                root = root.Parent;

            timeTables.Add(root);
            timeTables.AddRange(root.AllPlatformTimeTables);
            Console.WriteLine(timeTables.Count);
            var result = timeTables
                .SelectMany(entry => entry.GetNextDepartures(time))
                .GroupBy(x => x.Item2.Id)
                .Select(g => g.First())
                .OrderBy(x => x.Item1)
                .ToList();

            return TimeTable.PrintDepartures(result);
        }

        public bool DebugTripIsRunning(string tripId, DateTime time)
        {
            return _timeTableTrips.FirstOrDefault(e => e.Id == tripId).TripIsRunning(time);
        }

        public bool FindEarliestArrival(
            TimeTable source,
            TimeTable target,
            DateTime departureTime )
        {
            var allStops = _timeTables.Values;
            
            foreach (var stop in allStops)
            {
                stop.Label = new Label
                {
                    ArrivalTime = DateTime.MaxValue,
                    DepartureTime = DateTime.MaxValue
                };
            }

            source.Label.ArrivalTime = departureTime;

            bool improved;
            int round = 0;

            do
            {
                improved = false;
                round++;

                var markedStops = allStops
                    .Where(s => s.Label.ArrivalTime < DateTime.MaxValue)
                    .ToList();
                
                foreach (var stop in markedStops)
                {
                    var departures = stop.GetNeytUniqueDepartures(stop.Label.ArrivalTime);

                    foreach (var (depTime, trip) in departures)
                    {
                        var depDateTime = depTime.ToDateTimeFromTimeSpan(stop.Label.ArrivalTime);

                        if (depDateTime < stop.Label.ArrivalTime)
                            continue;

                        // Traverse stops along the trip
                        foreach (var stopTime in trip.Stops)
                        {
                            var arrival = stopTime.ArrivalTime
                                .ToDateTimeFromTimeSpan(depDateTime);

                            var nextStop = this._timeTables[stopTime.StopId];

                            if (arrival < nextStop.Label.ArrivalTime)
                            {
                                nextStop.Label.ArrivalTime = arrival;
                                nextStop.Label.ArrivedFrom = stop;
                                nextStop.Label.ArrivedWithTrip = trip;
                                improved = true;
                            }
                        }
                    }
                }

                // 3. Footpath relaxation
                foreach (var stop in allStops)
                {
                    if (stop.Label.ArrivalTime == DateTime.MaxValue)
                        continue;

                    foreach (var footStop in stop.ConnectedStations)
                    {
                        // add walking time here if needed
                        var arrival = stop.Label.ArrivalTime;

                        if (arrival < footStop.Label.ArrivalTime)
                        {
                            footStop.Label.ArrivalTime = arrival;
                            footStop.Label.ArrivedFrom = stop;
                            footStop.Label.ArrivedWithTrip = null;
                            improved = true;
                        }
                    }
                }
            } while (improved && round < 20); // safety cap

            return target.Label.ArrivalTime < DateTime.MaxValue;
        }
        
        public List<(TimeTable stop, TimeTableTrip trip)> ReconstructPath(Label targetLabel)
        {
            var path = new List<(TimeTable, TimeTableTrip)>();
            var current = targetLabel;

            while (current.ArrivedFrom != null)
            {
                path.Add((current.ArrivedFrom, current.ArrivedWithTrip));
                current = current.ArrivedFrom.Label;
            }

            path.Reverse();
            return path;
        }

        public TimeTable GetRandomStop()
        {
            return this._timeTables.Values.ToList()[new Random().Next(this._timeTables.Values.Count)];
        }
        
        public string PrintItinerary(Label targetLabel)
        {
            var path = ReconstructPath(targetLabel);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== Itinerary ===");

            DateTime? lastArrival = null;

            foreach (var (stop, trip) in path)
            {
                var label = stop.Label;

                // FOOTPATH
                if (trip == null)
                {
                    sb.AppendLine(
                        $"🚶 Walk to {stop.Stop.Name} " +
                        $"(arrive {label.ArrivalTime:HH:mm})");
                }
                else
                {
                    var depStop = trip.Stops
                        .Find(s => s.Stop.Id == stop.StopId);

                    var arrStop = trip.Stops.Last();

                    sb.AppendLine(
                        $"🚆 {trip.RouteName} ({trip.Id})");
                    sb.AppendLine(
                        $"   Depart {depStop.Stop.Name} at {label.DepartureTime:HH:mm}");
                    sb.AppendLine(
                        $"   Arrive {arrStop.Stop.Name} at {label.ArrivalTime:HH:mm}");
                }

                lastArrival = label.ArrivalTime;
            }

            sb.AppendLine("=================");
            sb.AppendLine($"Final arrival: {lastArrival:HH:mm}");

            return sb.ToString();
        }


    }
}