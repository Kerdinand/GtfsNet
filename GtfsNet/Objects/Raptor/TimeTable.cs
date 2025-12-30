using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GtfsNet.Helper.TimeTable;
using GtfsNet.Structs;

namespace GtfsNet.Objects.Raptor
{
    public class TimeTable
    {
        public TimeTable(Stop stop)
        {
            this._stop = stop;
        }
        
        private Stop _stop;
        public TimeTable Parent { get; set; }
        public string StopId =>  _stop.Id;
        public Stop Stop => _stop;
        public List<TimeTable> ConnectedStations { get; set; } = new List<TimeTable>();

        public List<(TimeSpan deptime,TimeTableTrip trip)> StoppingServices = new List<(TimeSpan,TimeTableTrip)>();

        public List<TimeTable> AllPlatformTimeTables = new List<TimeTable>();
        
        public Label Label { get; set; } = new Label();
        public void SortByDepartureTime()
        {
            StoppingServices.Sort((x, y) => x.deptime.CompareTo(y.deptime));
        }

        public List<(TimeSpan, TimeTableTrip)> GetNextDepartures(DateTime departureTime)
        {
             return StoppingServices.Where(service =>
                service.deptime.ToDateTimeFromTimeSpan(departureTime) >= departureTime &&
                service.trip.TripIsRunning(departureTime) && service.trip.Stops.Last().Stop.Id != _stop.Id).ToList();
        }

        public List<(TimeSpan, TimeTableTrip)> GetNeytUniqueDepartures(DateTime departureTime)
        {
            return StoppingServices.Where(service =>
                    service.deptime.ToDateTimeFromTimeSpan(departureTime) >= departureTime &&
                    service.trip.TripIsRunning(departureTime) && service.trip.Stops.Last().Stop.Id != _stop.Id)
                .GroupBy(service => service.trip.Route.Id)
                .Select(group =>
                    group.OrderBy(service => service.deptime).First())
                .ToList();
        }
        
        public List<(TimeSpan, TimeTableTrip)> GetNextArrivals(DateTime arrivalTime)
        {
            return StoppingServices.Where(service => 
                service.deptime.ToDateTimeFromTimeSpan(arrivalTime) >= arrivalTime && 
                service.trip.TripIsRunning(arrivalTime) && service.trip.Stops.First().Stop.Id != _stop.Id).ToList();
        }

        public static string PrintDepartures(List<(TimeSpan depTime, TimeTableTrip trip)> departures)
        {
            string result = "";
            foreach (var entry in departures)
            {
                result += $"@{entry.depTime}:{entry.trip.Stops.Find(_ => _.DepartureTime == entry.depTime).Stop.Name} - {entry.trip.RouteName} -> {entry.trip.Stops.Last().Stop.Name}, ID: {entry.trip.Id}, ServiceID: {entry.trip.Trip.ServiceId}\n";
                //result += entry.trip.PrintStops();
            }
            return result;
        }
        
        
    }
}