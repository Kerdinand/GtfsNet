using CsvHelper.Configuration.Attributes;

namespace GtfsNet.OSM.Graph;

public class OsmWay
{
    [Name("way_id")]
    public long WayId {get; set;}
    [Name("node_ids")]
    public string NodeIds {get; set;}
    [Name("tags")]
    public string Tags {get; set;}
}