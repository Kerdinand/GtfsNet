using System.Collections.Generic;
using System.Linq;
using GtfsNet.OSM.Rail;
using GtfsNet.Structs;
using KdTree;
using KdTree.Math;

namespace GtfsNet.OSM.KdTree;

public class OsmKdTree
{
    
    KdTree<double, OsmNode> _kdTree;
    List<OsmNode> _nodeList;
    
    public OsmKdTree(List<OsmNode> nodes)
    {
        this._nodeList = nodes;
        var tree = new KdTree<double, OsmNode>(2, new DoubleMath());

        foreach (var node in nodes)
        {
            tree.Add(new[] {node.Lat,node.Lon}, node);
        }
        this._kdTree = tree;
    }
    
    public OsmNode? FindNearest(Stop stop)
    {
        double[] queryPoint = { stop.Lat, stop.Lon };

        var nearest = _kdTree.GetNearestNeighbours(queryPoint, 1);

        // Might be empty
        if (nearest.Length == 0)
            return _nodeList[0] ?? null;

        // The result value is the OsmNode you stored
        return nearest[0].Value;
    }
    
    public List<OsmNode> FindNearestN(OsmNode stop, int k = 10)
    {
        double[] queryPoint = { stop.Lat, stop.Lon };

        var nearest = _kdTree.GetNearestNeighbours(queryPoint, k);

        if (nearest == null || nearest.Length == 0)
            return new List<OsmNode>();

        return nearest
            .Select(n => n.Value)
            .Where(n => n != null)
            .ToList();
    }

    
}
