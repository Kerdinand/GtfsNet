using System;

namespace GtfsNet.Routing.OsmStreetRouting;

public class OsmStreetNodeLabel
{

    public double Weight = double.MaxValue;
    public double Distance = 0;
    public OsmStreetNode Origin { get; set; } = null;
}