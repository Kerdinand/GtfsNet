using CsvHelper.Configuration.Attributes;

namespace GtfsNet.Structs
{
    public class Trip
    {
        [Name("trip_id")]
        public string Id { get; set; }
        [Name("service_id")]
        public string ServiceId { get; set; }
        [Name("route_id")]
        public string RouteId { get; set; }
        [Name("shape_id")]
        [Optional]
        public string ShapeId { get; set; }
        [Name("trip_headsign")]
        public string TripHeadsign { get; set; }
        [Name("trip_short_name")]
        [Optional]
        public string TripShortName { get; set; }
        [Name("direction_id")]
        public byte DirectionId { get; set; }
    }
}