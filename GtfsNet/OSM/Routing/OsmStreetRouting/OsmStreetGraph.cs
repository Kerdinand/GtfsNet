using System.Collections.Generic;
using System.Text.RegularExpressions;
using GtfsNet.Helper.Parsing;
using GtfsNet.OSM.Graph;
using GtfsNet.Routing.OsmStreetRouting;

namespace GtfsNet.OSM.Routing.OsmStreetRouting;

public class OsmStreetGraph
{
    Dictionary<long, OsmStreetNode> _nodes = new Dictionary<long, OsmStreetNode>();
    public int EdgeCount = 0;
    public Dictionary<long, OsmStreetNode> Nodes => _nodes;

    uint edgeId = 0;
    public bool AddNode(OsmNode node)
    {
        if (_nodes.ContainsKey(node.Id)) return false;
        _nodes.Add(node.Id, new OsmStreetNode(node));
        return true;
    }

    public OsmStreetNode GetNode(long nodeId)
    {
        return _nodes[nodeId];
    }

    public byte AddEdge(OsmNode source, OsmNode target, double distance, ushort speed, bool isOneWay)
    {
        if (!_nodes.ContainsKey(source.Id) || !_nodes.ContainsKey(target.Id)) return 0;
        var a = _nodes[source.Id];
        var b = _nodes[target.Id];
        var newEdgeA = new OsmStreetEdge(a, b, distance, speed);
        a.Edges.Add(newEdgeA);
        EdgeCount++;
        if (!isOneWay)
        {
            var newEdgeB = new OsmStreetEdge(b, a, distance,  speed);
            b.Edges.Add(newEdgeB);
            EdgeCount++;
            return 2;
        }
        return 1;
    }
}