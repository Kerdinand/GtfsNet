using System;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Enum;
using GtfsNet.Helper.Parsing;

namespace GtfsNet.Structs
{
    public class StopTime
    {
        [Name("trip_id")]
        public string TripId { get; set; }
        [Name("arrival_time")]
        [TypeConverter(typeof(GtfsTimeSpanConverter))]
        public TimeSpan ArrivalTime { get; set; }
        [Name("departure_time")]
        [TypeConverter(typeof(GtfsTimeSpanConverter))]
        public TimeSpan DepartureTime { get; set; }
        [Name("stop_id")]
        public string StopId { get; set; }
        [Name("stop_sequence")]
        public ushort Sequence {get; set;}
        [Name("stop_headsign")]
        public string StopHeadsign { get; set; }
        [Name("pickup_type")]
        public PickupType PickupTypeNumber { get; set; }
        [Name("drop_off_type")]
        public PickupType DropoffTypeNumber { get; set; }
        [Name("shape_dist_traveled")]
        [Optional]
        [Default(0f)]
        public float ShapeDistTraveled { get; set; }
        [Name("timepoint")]
        [Optional]
        public byte Timepoint { get; set; }
        [Ignore]
        public Stop Stop { get; set; }
    }
}