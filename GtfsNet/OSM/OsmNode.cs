using CsvHelper.Configuration.Attributes;

namespace GtfsNet.OSM;

public class OsmNode
{
    [Name("node_id")]
    public long Id { get; set; }
    [Name("node_lat")]
    public double Lat {get; set;}
    [Name("node_lon")]
    public double Lon {get; set;}

    override public string ToString()
    {
        return $"({Id} {Lat} {Lon})";
    }
}