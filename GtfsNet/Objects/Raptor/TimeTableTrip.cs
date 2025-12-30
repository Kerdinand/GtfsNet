using System;
using System.Collections.Generic;
using System.Linq;
using GtfsNet.Helper.TimeTable;
using GtfsNet.Structs;

namespace GtfsNet.Objects.Raptor
{
    public class TimeTableTrip
    {
        public List<StopTime> Stops { get; set; } = new List<StopTime>();
        private Trip _trip;
        private Route _route;
        
        public Trip Trip => _trip;
        public Route Route => _route;
        public string Id =>  _trip.Id;
        public string RouteName => _route.ShortName;
        
        public List<CalendarDate> CalendarDates { get; set; } = new();
        public Calendar Calendar { get; set; }

        public TimeTableTrip(Trip trip, Route route)
        {
            _trip = trip;
            _route = route;
        }

        public DateTime GetTripStartTime(DateTime dateTime)
        {
            return Stops[0].DepartureTime.ToDateTimeFromTimeSpan(dateTime);
        }

        public DateTime GetTripEndTime(DateTime dateTime)
        {
            return Stops[Stops.Count - 1].ArrivalTime.ToDateTimeFromTimeSpan(dateTime);
        }

        public bool TripIsRunning(DateTime dateTime)
        {
            // calendar_dates overrides calendar
            var cd = CalendarDates
                .FirstOrDefault(e => e.Date.Date == dateTime.Date);

            if (cd != null)
            {
                return cd.ExceptionType; // ONLY added
            }

            // fall back to weekly calendar
            return Calendar != null &&
                   Calendar.GetEntryForDayOfWeek(dateTime.DayOfWeek) && Calendar.IsInTimeFrame(dateTime);
        }


        public string PrintStops()
        {
            var result = "";

            foreach (var stop in Stops)
            {
                result += $"\t An: {stop.ArrivalTime} - {stop.Stop.Name} - {stop.DepartureTime}\n";
                
            }
            return result;
        }

    }
}