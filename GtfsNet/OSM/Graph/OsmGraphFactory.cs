using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using GtfsNet.Enum;
using GtfsNet.Helper.Parsing;
using GtfsNet.OSM.KdTree;
using GtfsNet.OSM.Routing.OsmStreetRouting;
using GtfsNet.Routing.OsmStreetRouting;
using QuikGraph;
using StringSplitOptions = System.StringSplitOptions;

namespace GtfsNet.OSM.Graph;

public class OsmGraphFactory
{

    public static BidirectionalGraph<long, OsmEdge> BuildBiderectionalGraph(
        Dictionary<long, OsmNode> nodeDict,
        Dictionary<long, OsmWay> ways,
        OsmType type,
        OsmKdTree kdTree,
        string filePath)
    {
        var graph = new BidirectionalGraph<long, OsmEdge>();

        // Always add vertices
        foreach (var nodeId in nodeDict.Keys)
            graph.AddVertex(nodeId);

        if (File.Exists(filePath))
        {
            using var reader = new StreamReader(filePath);
            reader.ReadLine(); // skip header

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 4)
                    continue;

                long source = long.Parse(parts[0]);
                long target = long.Parse(parts[1]);
                float weight = float.Parse(parts[2], CultureInfo.InvariantCulture);
                OsmType edgeType = parts[3].ToOsmType();

                graph.AddEdge(new OsmEdge(source, target, weight, edgeType));
            }

            Console.WriteLine($"Loaded graph from CSV: {graph.EdgeCount} edges");
            return graph;
        }

        // =========================================================
        // CASE 2: CSV DOES NOT EXIST → BUILD FROM OSM
        // =========================================================
        foreach (var way in ways)
        {
            var edgeNodes = way.Value.NodeIds
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
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
                    var m = Regex.Match(
                        way.Value.Tags,
                        @"(?:^|\s)maxspeed=(\d+)(?:\s*(?:km\/h|kph|mph))?(?=\s|$)"
                    );

                    weight /= m.Success
                        ? int.Parse(m.Groups[1].Value)
                        : 50;
                }

                graph.AddEdge(new OsmEdge(a, b, weight, type));

                if (!way.Value.Tags.Contains("oneway=yes"))
                    graph.AddEdge(new OsmEdge(b, a, weight, type));
            }
        }

        // KD-tree snapping (still allowed)
        if (kdTree is not null)
        {
            const double SNAP_DISTANCE = 1e-9;

            foreach (var node in nodeDict.Values)
            {
                var nearest = kdTree.FindNearestN(node, 30);

                foreach (var nearNode in nearest)
                {
                    if (nearNode.Id == node.Id)
                        continue;

                    if (OsmReader.Distance(node, nearNode) < SNAP_DISTANCE)
                    {
                        graph.AddEdge(new OsmEdge(node.Id, nearNode.Id, 0.00001f, type));
                        graph.AddEdge(new OsmEdge(nearNode.Id, node.Id, 0.00001f, type));
                    }
                }
            }
        }

        // =========================================================
        // WRITE CSV (FIRST-TIME BUILD ONLY)
        // =========================================================
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("source,target,weight,type");

            foreach (var edge in graph.Edges)
            {
                writer.WriteLine(
                    $"{edge.Source},{edge.Target},{edge.Weight.ToString(CultureInfo.InvariantCulture)},{edge.OsmType}"
                );
            }
        }

        Console.WriteLine($"Built graph from OSM: {graph.EdgeCount} edges");
        return graph;
    }


    public static BidirectionalGraph<long, OsmEdge> BuildBiderectionalGraph(Dictionary<long, OsmNode> nodeDict,
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
        //CollapseDegree2Nodes(graph);

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

            var inEdges = graph.InEdges(b).ToList();
            var outEdges = graph.OutEdges(b).ToList();

            if (inEdges.Count != 1 || outEdges.Count != 1)
                continue;

            var inEdge = inEdges[0]; // A → B
            var outEdge = outEdges[0]; // B → C

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

    public static OsmStreetGraph BuilCustomOsmGraph(Dictionary<long, OsmNode> nodeDict,
        Dictionary<long, OsmWay> ways)
    {
        var graph = new OsmStreetGraph();
        Console.WriteLine($"Building Osm Street Graph");
        foreach (var node in nodeDict.Values) graph.AddNode(node);
        Console.WriteLine($"Added {graph.Nodes.Values.Count} nodes");
        var existingEdges = new HashSet<string>();

        foreach (var way in ways.Values)
        {
            var m = Regex.Match(
                way.Tags,
                @"(?:^|\s)maxspeed=(\d+)(?:\s*(?:km\/h|kph|mph))?(?=\s|$)"
            );
            ushort speed = m.Success
                ? ushort.Parse(m.Groups[1].Value)
                : (ushort)50;
            bool isOneWay = way.Tags.Contains("oneway=yes");
            for (int i = 1; i < way.NodeIdArray.Length; i++)
            {
                var source = nodeDict[way.NodeIdArray[i - 1]];
                var target = nodeDict[way.NodeIdArray[i]];

                var distance = source.DistanceTo(target);
                var result = graph.AddEdge(source, target, distance, speed, isOneWay);
                existingEdges.Add($"{source.Id} - {target.Id}");
                if (result == 2) existingEdges.Add($"{target.Id} - {source.Id}");
            }
        }
        
        Console.WriteLine($"Added {graph.EdgeCount} edges");
        
        existingEdges.Clear();
        return graph;
    }

}