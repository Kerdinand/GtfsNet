using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Enum;
using GtfsNet.OSM;
using SQLite;

namespace GtfsNet.Structs
{
    [Table("Stops")]
    public class Stop
    {
        [Name("stop_id")]
        [PrimaryKey]
        public string Id { get; set; }
        [Name("stop_code")]
        [Optional]
        public string Code { get; set; }
        [Name("stop_name")]
        [Column("stop_name")]
        public string Name { get; set; }
        [Name("stop_lat")]
        [Column("stop_lat")]
        public double Lat { get; set; }
        [Name("stop_lon")]
        [Column("stop_lon")]
        public double Lon { get; set; }
        [Name("stop_url")]
        [Optional]
        public string Url { get; set; }
        [Name("parent_station")]
        [Column("parent_station")]
        public string ParentStation { get; set; }
        [Name("location_type")]
        public byte? Type { get; set; }
        [Name("platform_code")]
        [Optional]
        public string PlatformCode { get; set; }

        /// <summary>
        /// Location type is mapped on byte of type entry
        /// </summary>
        [CsvHelper.Configuration.Attributes.Ignore]
        [SQLite.Ignore]
        public LocationType LocationType => (LocationType)Type;
        
        [CsvHelper.Configuration.Attributes.Ignore]
        [SQLite.Ignore]
        public List<(OsmType, OsmNode)> OsmNode {get; set;}
        
        [CsvHelper.Configuration.Attributes.Ignore]
        [SQLite.Ignore]
        public OsmNode roadNode { get; set; }
        [Column("osm_road_id")]
        [CsvHelper.Configuration.Attributes.Ignore]
        public long OsmRoadNodeId {get => roadNode?.Id ?? -1;
            set
            {
                OsmRoadNodeId = value;
            }
        }
    }
}