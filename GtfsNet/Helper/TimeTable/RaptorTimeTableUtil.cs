using System;
using System.Collections.Generic;

namespace GtfsNet.Helper.TimeTable
{
    public static class RaptorTimeTableUtil
    {
        public static DateTime ToDateTimeFromTimeSpan(this TimeSpan timeSpan, DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, timeSpan.Hours, timeSpan.Minutes,
                timeSpan.Seconds);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this List<TValue> values,
            Func<TValue, TKey> keySelector)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (var entry in values)
            {
                var key = keySelector(entry);
                dictionary.Add(key, entry);
            }
            return dictionary;
        }

        public static (double lat, double lon) GetPosition(this Objects.Raptor.TimeTable timeTable)
        {
            return (timeTable.Stop.Lat,  timeTable.Stop.Lon);
        }

        public static float GetDistance(
            this Objects.Raptor.TimeTable timeTable,
            Objects.Raptor.TimeTable otherTimeTable)
        {
            const double EarthRadiusMeters = 6371000.0;

            double lat1 = DegreesToRadians(timeTable.Stop.Lat);
            double lon1 = DegreesToRadians(timeTable.Stop.Lon);
            double lat2 = DegreesToRadians(otherTimeTable.Stop.Lat);
            double lon2 = DegreesToRadians(otherTimeTable.Stop.Lon);

            double dLat = lat2 - lat1;
            double dLon = lon2 - lon1;

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (float)(EarthRadiusMeters * c);
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

    }
}