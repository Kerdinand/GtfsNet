using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GtfsNet.Enum;
using GtfsNet.Helper.Parsing;
using GtfsNet.OSM.KdTree;
using GtfsNet.OSM.Rail;
using QuikGraph;
using StringSplitOptions = System.StringSplitOptions;

namespace GtfsNet.OSM.Graph;

public class OsmGraphFactory
{
    
    public static BidirectionalGraph<long, OsmEdge> BuildAdjacencyGraph(Dictionary<long, OsmNode> nodeDict,
        Dictionary<long, OsmWay> ways, OsmType type, OsmKdTree kdTree = null)
    {
        var graph = new BidirectionalGraph<long, OsmEdge>();

        // Add vertices
        foreach (var nodeId in nodeDict.Keys)
            graph.AddVertex(nodeId);

        // Add edges
        foreach (var way in ways)
        {
            var edgeNodes = way.Value.NodeIds.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(long.Parse)
                .ToList();
            
            for (int i = 1; i < edgeNodes.Count; i++)
            {
                var a = edgeNodes[i - 1];
                var b = edgeNodes[i];

                if (!nodeDict.ContainsKey(a) || !nodeDict.ContainsKey(b))
                    continue;

                float weight = (float)nodeDict[a].DistanceTo(nodeDict[b]);
                if (way.Value.Tags.Contains("maxspeed"))
                {
                    var m = Regex.Match(way.Value.Tags, @"(?:^|\s)maxspeed=(\d+)(?:\s*(?:km\/h|kph|mph))?(?=\s|$)");
                    if (m.Success)
                    {
                        var speed = int.Parse(m.Groups[1].Value);
                        weight = weight / speed;
                    }
                    else
                    {
                        weight = weight / 50;
                    }
                }
                graph.AddEdge(new OsmEdge(a, b, weight, type));
                if (!way.Value.Tags.Contains("oneway=yes"))
                {
                    graph.AddEdge(new OsmEdge(b, a, weight, type));
                }

                
            }
        }
        
        if (kdTree is not null)
        {
            foreach (var node in nodeDict.Values)
            {
                var nearest = kdTree.FindNearestN(node, 30);
                foreach (var nearNode in nearest)
                {
                    if (nearNode.Id == node.Id) continue;
                    double SNAP_DISTANCE = 1e-6;
                    if (OsmReader.Distance(node, nearNode) < SNAP_DISTANCE)
                    {
                        var source = node.Id;
                        var target = nearNode.Id;
                        graph.AddEdge(new OsmEdge(source, target, 0.00001f, type));
                        graph.AddEdge(new OsmEdge(target, source, 0.00001f, type));
                    }
                }
            }
        }
        var edgeCount = graph.EdgeCount;
        CollapseDegree2Nodes(graph);
        
        Console.WriteLine($"{edgeCount} reduced to {graph.EdgeCount}");
        return graph;
    }
    
    public static void CollapseDegree2Nodes(BidirectionalGraph<long, OsmEdge> graph)
    {
        // Snapshot because we mutate the graph
        var vertices = graph.Vertices.ToList();

        foreach (var b in vertices)
        {
            if (!graph.ContainsVertex(b))
                continue;

            var inEdges  = graph.InEdges(b).ToList();
            var outEdges = graph.OutEdges(b).ToList();

            if (inEdges.Count != 1 || outEdges.Count != 1)
                continue;

            var inEdge  = inEdges[0];   // A → B
            var outEdge = outEdges[0];  // B → C

            var a = inEdge.Source;
            var c = outEdge.Target;

            // Prevent self-loops
            if (a == c)
                continue;

            // Combine weights
            var combinedWeight = inEdge.Weight + outEdge.Weight;

            // Add collapsed edge A → C
            graph.AddEdge(new OsmEdge(a, c, combinedWeight, inEdge.OsmType));

            // Remove old edges and node
            graph.RemoveEdge(inEdge);
            graph.RemoveEdge(outEdge);
            graph.RemoveVertex(b);
        }
    }



}