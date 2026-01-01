using System;
using System.Collections.Generic;
using System.Linq;
using GtfsNet.OSM.Graph;
using GtfsNet.Routing.OsmStreetRouting;

namespace GtfsNet.OSM.Routing.OsmStreetRouting;

public class Dijkstra
{
    private byte _id;
    private OsmStreetGraph _graph;
    private HashSet<OsmStreetNode> _visitedNodes = new HashSet<OsmStreetNode>();
    public Dijkstra(byte id, OsmStreetGraph graph)
    {
        this._id = id;
        this._graph = graph;
    }

    public void RelaxAll()
    {
        foreach (var node in _graph.Nodes.Values)
        {
            if (node.Labels.TryGetValue(_id, out var label))
            {
                label.Weight = float.MaxValue;
                label.Origin = null;
            }
            else
            {
                node.Labels.Add(_id, new OsmStreetNodeLabel());
            }
        }
    }
    
    private void Relax(HashSet<OsmStreetNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Labels.TryGetValue(_id, out var label))
            {
                label.Weight = float.MaxValue;
                label.Origin = null;
            }
            else
            {
                node.Labels.Add(_id, new OsmStreetNodeLabel());
            }
        }
    }

    public List<OsmNode> GetRoute(long source, long target, double maxDistance = double.MaxValue, double UTurnPenalty = 500)
    {
        Relax(_visitedNodes);
        _visitedNodes.Clear();
        //Console.WriteLine($"Dijkstra {_id} commenced routing");
        var sourceNode = _graph.GetNode(source);
        var targetNode = _graph.GetNode(target);
        sourceNode.Labels[_id].Weight = 0;
        sourceNode.Labels[_id].Distance = 0;
        var nodesToExplore = new PriorityQueue<OsmStreetNode, double>();
        nodesToExplore.Enqueue(sourceNode, sourceNode.Labels[_id].Weight);
        var targetFound = false;
        OsmStreetNode previousNode = null;
        
        while (nodesToExplore.Count > 0)
        {
            var currentSourceNode = nodesToExplore.Dequeue();
            _visitedNodes.Add(currentSourceNode);
            if (currentSourceNode.Id ==  target)
            {
                targetFound=true;
                break;
            };
            var nodeDistance = currentSourceNode.Labels[_id].Distance;
            foreach (var edge in currentSourceNode.Edges)
            {
                var newWeight = currentSourceNode.Labels[_id].Weight + edge.Weight;
                var newDistnace = nodeDistance + edge.Distance;
                var extraCost = 0d;
                if (IsUTurn(currentSourceNode, edge.Target))
                    continue;
                
                
                if (newDistnace > maxDistance)
                    continue;

                if (newWeight < edge.Target.Labels[_id].Weight)
                {
                    edge.Target.Labels[_id].Weight = newWeight;
                    edge.Target.Labels[_id].Origin = currentSourceNode;
                    edge.Target.Labels[_id].Distance = newDistnace;
                    nodesToExplore.Enqueue(edge.Target, newWeight);
                }
            }
            
            if (_visitedNodes.Count > _graph.Nodes.Count) break;
        }

        if (!targetFound)
        {
            //Console.WriteLine($"Dijkstra {_id} routing failed");
            return new List<OsmNode>();
        }
        
        
        var currentNode = targetNode;
        var result = new List<OsmNode>(){}; 
        var coveredIds = new HashSet<long>(){};
        int counter = 0;
        while (currentNode != null && counter < _graph.Nodes.Count)
        {
            if (coveredIds.Contains(currentNode.OsmNode.Id))
            {
                break;
            }
            result.Add(currentNode.OsmNode);
            coveredIds.Add(currentNode.OsmNode.Id);
            currentNode = currentNode.Labels[_id].Origin;
            counter++;
        }
        result.Reverse();
        //Console.WriteLine($"Dijkstra {_id} routing successful");

        return result;
    }
    
    private  (double x, double y) Direction(OsmNode from, OsmNode to)
    {
        var dx = to.Lon - from.Lon;
        var dy = to.Lat - from.Lat;
        var len = Math.Sqrt(dx * dx + dy * dy);
        return len == 0 ? (0, 0) : (dx / len, dy / len);
    }
    
    private bool IsUTurn(OsmStreetNode current, OsmStreetNode next)
    {
        var prev = current.Labels[_id].Origin;
        if (prev == null) return false;

        var v1 = Direction(prev.OsmNode, current.OsmNode);
        var v2 = Direction(current.OsmNode, next.OsmNode);

        // dot ≈ -1 → opposite direction
        var dot = v1.x * v2.x + v1.y * v2.y;

        return dot < -0.8; // ≈ >145° turn
    }


}