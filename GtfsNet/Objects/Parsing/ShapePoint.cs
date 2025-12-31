using CsvHelper.Configuration.Attributes;

namespace GtfsNet.Factories;

public class ShapePoint
{
    [Name("shape_id")]
    public string ShapeId { get;  set; }
    [Name("shape_pt_lat")]
    public float Lat { get;  set; }
    [Name("shape_pt_lon")]
    public float Lon { get;  set; }
    [Name("shape_pt_sequence")]
    public long Sequence { get;  set; }
    [Name("shape_dist_traveled")]
    public float DistTraveled { get;  set; }
}