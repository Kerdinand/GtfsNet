using System.Drawing;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Helper.Parsing;

namespace GtfsNet.Structs
{
    public class Route
    {
        [Name("route_id")]
        public string Id { get; set; }
        [Name("agency_id")]
        public string AgencyId { get; set; }
        [Name("route_short_name")]
        public string ShortName { get; set; }
        [Name("route_long_name")]
        public string LongName { get; set; }
        [Name("route_type")]
        [TypeConverter(typeof(GtfsByteConverter))]
        public byte RouteType { get; set; }
        [Name("route_color")]
        public string RouteColor { get; set; }
        [Name("route_text_color")]
        public string RouteTextColor { get; set; }
        [Name("route_url")]
        [Optional]
        public string RouteUrl { get; set; }
        [Name("ticketing_deep_link_id")]
        [Optional]
        public string TicketingDeepLinkId { get; set; }
    }
}