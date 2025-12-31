using System.Collections.Generic;
using GtfsNet.OSM;

namespace GtfsNet.Routing.OsmStreetRouting;

public class OsmStreetNode(OsmNode node)
{
    public long Id => node.Id;
    public List<OsmStreetEdge> Edges { get; set; } = new List<OsmStreetEdge>();
    public Dictionary<byte, OsmStreetNodeLabel> Labels { get; set; } = new Dictionary<byte, OsmStreetNodeLabel>();
    public OsmNode OsmNode { get; set; } = node;
}