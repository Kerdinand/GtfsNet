using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Enum;
using GtfsNet.OSM.Rail;

namespace GtfsNet.Structs
{
    public class Stop
    {
        [Name("stop_id")]
        public string Id { get; set; }
        [Name("stop_code")]
        [Optional]
        public string Code { get; set; }
        [Name("stop_name")]
        public string Name { get; set; }
        [Name("stop_lat")]
        public double Lat { get; set; }
        [Name("stop_lon")]
        public double Lon { get; set; }
        [Name("stop_url")]
        public string Url { get; set; }
        [Name("parent_station")]
        public string ParentStation { get; set; }
        [Name("location_type")]
        public byte? Type { get; set; }
        [Name("platform_code")]
        [Optional]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Location type is mapped on byte of type entry
        /// </summary>
        [Ignore]
        public LocationType LocationType => (LocationType)Type;
        
        [Ignore]
        public List<(OsmType, OsmNode)> OsmNode {get; set;}
    }
}