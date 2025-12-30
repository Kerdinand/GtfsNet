using System;
using System.Collections.Generic;
using System.Linq;
using GtfsNet.Helper.TimeTable;
using GtfsNet.Objects.Raptor;
using GtfsNet.Structs;

namespace GtfsNet.Factories
{
    public class RaptorFactory
    {
        GtfsFeed _gtfsFeed;
        
        public RaptorFactory(GtfsFeed gtfsFeed)
        {
            _gtfsFeed = gtfsFeed;
        }

        public Raptor BuildRaptor()
        {
            var timeTables = CreateTimeTables();
            SetStopsToStopTimes(_gtfsFeed.Stops, _gtfsFeed.StopTimes);
            var timeTableTrips = SetStopsToStopTimes(_gtfsFeed.Stops, _gtfsFeed.StopTimes);
            SetTimeTableTripsToTimeTables(timeTableTrips, timeTables);
            foreach (var stop in _gtfsFeed.Stops.ToDictionary(_ => _.Id).Values)
            {
                if (stop.ParentStation != "")
                {
                    timeTables[stop.Id].Parent =  timeTables[stop.ParentStation];
                    timeTables[stop.ParentStation].AllPlatformTimeTables.Add(timeTables[stop.Id]);
                }
            }
            SetCalendarsToTimeTableTrips(timeTableTrips.Values.ToList());
            ConnectCloseStations(timeTables.Values.ToList(), 100);
            return new Raptor(timeTables, timeTableTrips.Values.ToList());

            void ConnectCloseStations(IList<TimeTable> timeTables, float distance)
            {
                for (int i = 0; i < timeTables.Count; i++)
                {
                    for (int j = i + 1; j < timeTables.Count; j++)
                    {
                        float d = 0f;
                        if ((d = timeTables[i].GetDistance(timeTables[j])) < distance)
                        {
                            timeTables[i].ConnectedStations.Add(timeTables[j]);
                            timeTables[j].ConnectedStations.Add(timeTables[i]);
                        }
                    }
                }
            }
        }

        private Dictionary<string,TimeTable> CreateTimeTables()
        {
            var timeTableDictionary = new Dictionary<string, TimeTable>();

            foreach (var stop in _gtfsFeed.Stops)
            {
                timeTableDictionary.Add(stop.Id, new TimeTable(stop));
            }
            return timeTableDictionary;
        }

        public Dictionary<string, TimeTableTrip> SetStopsToStopTimes(List<Stop> stops, List<StopTime> stopTimes)
        {
            var stopDict = stops.ToDictionary(_ => _.Id);
            var stopTimeDict = stopTimes.GroupBy(_ => _.TripId).Select(_ => _.ToList()).ToDictionary(_ => _[0].TripId);
            var routeDict = _gtfsFeed.Route.ToDictionary(_ => _.Id);
            
            foreach (var sT in stopTimeDict.Values)  foreach (var stop in sT) stop.Stop =  stopDict[stop.StopId];
            
            
            Dictionary<string, TimeTableTrip> timeTableTrips = _gtfsFeed.Trips.Select(t => new TimeTableTrip(t,routeDict[t.RouteId])).ToDictionary(_ => _.Id);

            foreach (var tT in timeTableTrips.Values) tT.Stops = stopTimeDict[tT.Id];
            return timeTableTrips;
        }

        private void SetTimeTableTripsToTimeTables(Dictionary<string, TimeTableTrip> timeTableTrips, Dictionary<string, TimeTable> timeTables)
        {
            foreach (var tT in timeTableTrips.Values)
            {
                foreach (var stop in tT.Stops)
                {
                    timeTables[stop.StopId].StoppingServices.Add((stop.DepartureTime,tT));
                }
            }
            foreach (var stops in timeTables.Values) stops.SortByDepartureTime();
        }

        private void SetCalendarsToTimeTableTrips(List<TimeTableTrip> timeTableTrips)
        {
            var calendarDict = _gtfsFeed.Calendar.ToDictionary(_ => _.ServiceId);
            var calendarDatesDict = _gtfsFeed.CalendarDate.GroupBy(_ => _.ServiceId).Select(_ => _.ToList()).ToDictionary(_ => _[0].ServiceId);

            foreach (var tT in timeTableTrips)
            {
                tT.Calendar = calendarDict[tT.Trip.ServiceId];
                if (calendarDatesDict.ContainsKey(tT.Trip.ServiceId)) tT.CalendarDates = calendarDatesDict[tT.Trip.ServiceId];
            }
        }
    }
}