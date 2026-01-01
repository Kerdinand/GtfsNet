using CsvHelper.Configuration.Attributes;

namespace GtfsNet.OSM;

public class OsmShapeDto
{
    [Name("lat")]
    public float Lat {get; set;}
    [Name("lon")]
    public float Lon {get; set;}
    [Name("seq")]
    public ushort Sequence {get; set;}
}