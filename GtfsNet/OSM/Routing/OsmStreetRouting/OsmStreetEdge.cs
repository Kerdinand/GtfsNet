using System;
using CsvHelper.Configuration.Attributes;

namespace GtfsNet.Routing.OsmStreetRouting;

public class OsmStreetEdge
{
    [Name("target")]
    public OsmStreetNode Target { get; set; }
    [Name("weight")]
    public double Weight { get; set; }
    public double Distance { get; set; }
    public OsmStreetEdge(OsmStreetNode source, OsmStreetNode target,double distance, ushort speed, byte priority = 1)
    {
        Target = target;
        Weight = distance / (speed * 10);
        Distance = distance;
    }
}