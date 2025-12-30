using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using GtfsNet.Structs;
using Calendar = GtfsNet.Structs.Calendar;

namespace GtfsNet
{
    public static class Gtfs
    {
        
        private static GtfsFeed _feed = new GtfsFeed();
        
        public static GtfsFeed ReadFromCsv(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException(folderPath);

            var feed = new GtfsFeed();

            var stopsTask = Task.Run(() =>
                CsvRead<Stop>(Path.Combine(folderPath, "stops.txt")));

            var stopTimesTask = Task.Run(() =>
                CsvRead<StopTime>(Path.Combine(folderPath, "stop_times.txt")));

            var tripsTask = Task.Run(() =>
                CsvRead<Trip>(Path.Combine(folderPath, "trips.txt")));
            var calendarTask = Task.Run(() =>
                CsvRead<Calendar>(Path.Combine(folderPath, "calendar.txt")));
            var calendarDateTask = Task.Run(() => 
                CsvRead<CalendarDate>(Path.Combine(folderPath, "calendar_dates.txt")));
            var routeTask = Task.Run(() =>
                CsvRead<Route>(Path.Combine(folderPath, "routes.txt")));

            Task.WhenAll(stopsTask, stopTimesTask, tripsTask, calendarTask, calendarDateTask, routeTask).Wait();

            feed.Stops = stopsTask.Result;
            feed.StopTimes = stopTimesTask.Result;
            feed.Trips = tripsTask.Result;
            feed.Calendar = calendarTask.Result;
            feed.CalendarDate = calendarDateTask.Result;
            feed.Route = routeTask.Result;

            _feed = feed;
            
            return feed;
        }

        private static List<T> CsvRead<T>(string path)
        {
            if (!File.Exists(path))
                return new List<T>();

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<T>().ToList();
            }
        }
    }

}