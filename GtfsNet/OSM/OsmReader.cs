using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CsvHelper;
using GtfsNet.Enum;
using GtfsNet.Helper.Parsing;
using GtfsNet.OSM.Graph;
using GtfsNet.OSM.KdTree;
using GtfsNet.OSM.Routing.OsmStreetRouting;
using GtfsNet.Routing.OsmStreetRouting;
using GtfsNet.Structs;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.IntervalRTree;
using OsmSharp;
using OsmSharp.API;
using OsmSharp.Streams;
using QuikGraph;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.ShortestPath;

namespace GtfsNet.OSM;

public class OsmReader
{
    private string _path;
    private GtfsFeed _gtfsFeed;
    private string osmFile = "/schleswig-holstein-251229.osm.pbf";
    public OsmReader(string path, string osmFile, GtfsFeed feed)
    {
        Console.WriteLine($"Initializing {nameof(OsmReader)}");
        this._path = path;
        this._gtfsFeed = feed;
        this.osmFile = osmFile;
        _routeDict = this._gtfsFeed.Route.ToDictionary(r => r.Id);
        _stopTimesDict = _gtfsFeed.StopTimesDictionary();
    }

    private Dictionary<long, OsmWay> highwayWays = new Dictionary<long, OsmWay>();
    private HashSet<long> higwayNodes = new HashSet<long>();
    private Dictionary<long, OsmNode> higwayNodesDict = new Dictionary<long, OsmNode>();
    private Dictionary<long, OsmWay> railWays = new Dictionary<long, OsmWay>();
    private HashSet<long> railNodes = new HashSet<long>();
    private Dictionary<long, OsmNode> railNodesDict = new Dictionary<long, OsmNode>();
    private Dictionary<long, OsmWay> tramWays = new Dictionary<long, OsmWay>();
    private HashSet<long> tramNodes = new HashSet<long>();
    private Dictionary<long, OsmNode> tramNodesDict = new Dictionary<long, OsmNode>();
    private Dictionary<long, OsmWay> lightRailWays = new Dictionary<long, OsmWay>();
    private HashSet<long> lightRailNodes = new HashSet<long>();
    private Dictionary<long, OsmNode> lightRailNodesDict = new Dictionary<long, OsmNode>();
    private Dictionary<long, OsmWay> subwayWays = new Dictionary<long, OsmWay>();
    private HashSet<long> subwayNodes = new HashSet<long>();
    private Dictionary<long, OsmNode> subwayNodesDict = new Dictionary<long, OsmNode>();

    private BidirectionalGraph<long, OsmEdge> highwayGraph;
    private BidirectionalGraph<long, OsmEdge> railGraph;
    private BidirectionalGraph<long, OsmEdge> lightRailGraph;
    private BidirectionalGraph<long, OsmEdge> tramGraph;
    private BidirectionalGraph<long, OsmEdge> subwayGraph;
    private OsmKdTree _tramTree;
    private OsmKdTree _subwayTree;
    private OsmKdTree _railTree;
    private OsmKdTree _lightRailTree;
    private OsmKdTree _highwayTree;
    private Dictionary<string, Route> _routeDict;
    private Dictionary<string, List<StopTime>> _stopTimesDict;

    private Dictionary<string, SubLinkDto> _precomputedShapes;
    
    private List<Trip>? _cachedBusTrips;
    private readonly object _busTripLock = new();
    private Dictionary<string, Stop> _stopDict;

    public List<Stop> GetStopsOfRandomBusTrip()
    {
        if (_cachedBusTrips == null)
        {
            lock (_busTripLock)
            {
                if (_cachedBusTrips == null)
                {
                    var busRouteIds = _routeDict.Values
                        .Where(r => r.RouteType == 3)
                        .Select(r => r.Id)
                        .ToHashSet();

                    _cachedBusTrips = _gtfsFeed.Trips
                        .Where(t => busRouteIds.Contains(t.RouteId))
                        .ToList();
                }
            }
        }

        if (_cachedBusTrips.Count == 0)
            return new List<Stop>();

        // 🎲 Pick random trip
        var trip = _cachedBusTrips[Random.Shared.Next(_cachedBusTrips.Count)];

        if (!_stopTimesDict.TryGetValue(trip.Id, out var stopTimes))
            return new List<Stop>();

        return stopTimes
            .OrderBy(st => st.Sequence)
            .Select(st => st.Stop)
            .ToList();
    }


    private void WriteWayAndAddNodesToSet(StreamWriter writer, Way way, HashSet<long> nodesToWrite,
        bool ignoreInnerNodes = false, int takeEveryNthNode = 0)
    {
        long wayId = way.Id ?? -1;

        var nodes = way.Nodes.Select(e => (long)e).ToArray();
        IEnumerable<long> nodesToWriteToCsv;
        int counter = 0;
        if (takeEveryNthNode > 1)
        {
            var temp = new List<long>();
            temp.Add(nodes[0]);
            for (int i = takeEveryNthNode; i < nodes.Count() - 1; i += takeEveryNthNode)
            {
                temp.Add(nodes[i]);
            }

            if (nodes.Count() > 1) temp.Add(nodes[nodes.Count() - 1]);
            nodesToWriteToCsv = temp;
        }
        else
        {
            if (ignoreInnerNodes && nodes.Length >= 2)
            {
                // Keep only first and last node
                nodesToWriteToCsv = new[] { nodes[0], nodes[nodes.Length - 1] };
            }
            else
            {
                nodesToWriteToCsv = nodes;
            }
        }

        foreach (var nodeId in nodesToWriteToCsv) nodesToWrite.Add(nodeId);

        string nodeIds = string.Join(" ", nodesToWriteToCsv);

        string tags = string.Join(" ", way.Tags.Select(t => $"{t.Key}={t.Value}")).Replace("\"", "\"\"");

        writer.WriteLine($"{wayId},{nodeIds},\"{tags}\"");
    }

    public Dictionary<long, OsmNode> GetOsmNodes(OsmType type)
    {
        return GetNodeDictionary(type);
    }

    public Dictionary<long, OsmWay> GetOsmWays(OsmType type)
    {
        return GetWayDictionary(type);
    }

    public void WriteCsv(string outputPath, bool rewrite = false)
    {
        if (!rewrite && File.Exists(outputPath + "/HighwayNodes.csv"))
        {
            Console.WriteLine("Nodes and Ways csv already exist. Skipping writing of files.");
            return;
        }
        var allowedHighways = new HashSet<string>
        {
            "motorway", "motorway_link",
            "trunk", "trunk_link",
            "primary", "primary_link",
            "secondary", "secondary_link",
            "tertiary", "tertiary_link",
            "residential",
            "unclassified",
            "road",
            "busway"
        };

        Console.WriteLine($"Commencing writing Ways");

        using (var fs = File.OpenRead(_path + osmFile))
        using (var highwayWayWriter = new StreamWriter(outputPath + "/HighwayWays.csv"))
        using (var tramWayWriter = new StreamWriter(outputPath + "/TramWays.csv"))
        using (var railWayWriter = new StreamWriter(outputPath + "/RailWays.csv"))
        using (var lightRailWayWriter = new StreamWriter(outputPath + "/LightRailWays.csv"))
        using (var subwayWriter = new StreamWriter(outputPath + "/SubwayWays.csv"))
        {
            var source = new PBFOsmStreamSource(fs);
            highwayWayWriter.WriteLine("way_id,node_ids,tags");
            railWayWriter.WriteLine("way_id,node_ids,tags");
            tramWayWriter.WriteLine("way_id,node_ids,tags");
            subwayWriter.WriteLine("way_id,node_ids,tags");
            lightRailWayWriter.WriteLine("way_id,node_ids,tags");
            foreach (var entry in source)
            {
                if (entry is not Way way) continue;
                if (way.Tags is null) continue;
                if (way.Tags.TryGetValue("highway", out var highway) &&
                    allowedHighways.Contains(highway))
                {
                        WriteWayAndAddNodesToSet(highwayWayWriter, way, higwayNodes, false, 1);
                }

                if (way.Tags.Contains("railway", "rail"))
                {
                    WriteWayAndAddNodesToSet(railWayWriter, way, railNodes);
                }

                if (way.Tags.Contains("railway", "light_rail"))
                {
                    WriteWayAndAddNodesToSet(lightRailWayWriter, way, lightRailNodes);
                }

                if (way.Tags.Contains("railway", "tram"))
                {
                    WriteWayAndAddNodesToSet(tramWayWriter, way, tramNodes);
                }

                if (way.Tags.Contains("railway", "subway"))
                {
                    WriteWayAndAddNodesToSet(subwayWriter, way, subwayNodes);
                }
            }
        }

        Console.WriteLine("Finished writing ways and setting to hashsets");
        Console.WriteLine($"Commencing of Writing Nodes");
        using (var fs = File.OpenRead(_path + osmFile))
        using (var highwayNodeWriter = new StreamWriter(outputPath + "/HighwayNodes.csv"))
        using (var tramNodeWriter = new StreamWriter(outputPath + "/TramNodes.csv"))
        using (var railNodeWriter = new StreamWriter(outputPath + "/RailNodes.csv"))
        using (var lightRailNodeWriter = new StreamWriter(outputPath + "/LightRailNodes.csv"))
        using (var subwayNodeWriter = new StreamWriter(outputPath + "/SubwayNodes.csv"))
        {
            var source = new PBFOsmStreamSource(fs);
            highwayNodeWriter.WriteLine("node_id,node_lat,node_lon");
            tramNodeWriter.WriteLine("node_id,node_lat,node_lon");
            railNodeWriter.WriteLine("node_id,node_lat,node_lon");
            lightRailNodeWriter.WriteLine("node_id,node_lat,node_lon");
            subwayNodeWriter.WriteLine("node_id,node_lat,node_lon");

            foreach (var entry in source)
            {
                if (entry is not Node node) continue;
                if (!(node.Id.HasValue && node.Latitude.HasValue && node.Longitude.HasValue)) continue;
                if (higwayNodes.Contains((long)node.Id.Value))
                {
                    highwayNodeWriter.WriteLine($"{node.Id.Value},{node.Latitude.Value},{node.Longitude.Value}");
                }

                if (tramNodes.Contains((long)node.Id.Value))
                {
                    tramNodeWriter.WriteLine($"{node.Id.Value},{node.Latitude.Value},{node.Longitude.Value}");
                }

                if (railNodes.Contains((long)node.Id.Value))
                {
                    railNodeWriter.WriteLine($"{node.Id.Value},{node.Latitude.Value},{node.Longitude.Value}");
                }

                if (lightRailNodes.Contains((long)node.Id.Value))
                {
                    lightRailNodeWriter.WriteLine($"{node.Id.Value},{node.Latitude.Value},{node.Longitude.Value}");
                }

                if (subwayNodes.Contains((long)node.Id.Value))
                {
                    subwayNodeWriter.WriteLine($"{node.Id.Value},{node.Latitude.Value},{node.Longitude.Value}");
                }
            }
        }

        Console.WriteLine($"Finished of with writing Nodes");
    }

    public void SetStopsToStopTimes()
    {
        var stopDict = _gtfsFeed.Stops.ToDictionary(stop => stop.Id, stop => stop);
        foreach (var stopTime in _gtfsFeed.StopTimes) stopTime.Stop = stopDict[stopTime.StopId];
    }

    public void SetClosestOsmNodeForGtfsStops(List<OsmStreetNode> sourceNode, List<Stop> stops, bool force = false)
    {
        if (!File.Exists(_path + "/gtfs2osmroad.csv") || force)
        {
            Console.WriteLine("KD Tree created for sourceNodes");
            var nodes = sourceNode.Where(n => n.Edges.Count != 0).Select(n => n.OsmNode).ToList();
            Console.WriteLine($"Reduced possible nodes to: {nodes.Count}");
            var sourceKDtree = new OsmKdTree(nodes);
            foreach (var stop in stops)
            {
                var closestNode = sourceKDtree.FindNearest(stop);
                if (closestNode is not null) stop.roadNode = closestNode;
            }

            using (var writer = new StreamWriter(_path + "/gtfs2osmroad.csv"))
            {
                writer.WriteLine("gtfsId,osmIds");
                foreach (var stop in stops) writer.WriteLine($"{stop.Id},{stop.roadNode.Id}");
            }
        }
        else
        {
            Console.WriteLine("Mapping Found! Skipping generation!");
            using (var reader = new  StreamReader(_path + "/gtfs2osmroad.csv"))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                _stopDict = _gtfsFeed.Stops.ToDictionary(stop => stop.Id, stop => stop);
                var records = csvReader.GetRecords<RawRecord>();
                foreach (var record in records)
                {
                    _stopDict[record.GtfsId].roadNode = higwayNodesDict[long.Parse(record.OsmIdsRaw)];
                }
            }
        }
        
    }

    public void SetClosestOsmNodeForGtfsNodes()
    {
        if (File.Exists(_path + "/gtfs2osm.csv"))
        {
            using (var tramNodeReader = new StreamReader(_path + "/TramNodes.csv"))
            using (var tramNodeCsvReader = new CsvReader(tramNodeReader, CultureInfo.InvariantCulture))
            using (var subwayNodeReader = new StreamReader(_path + "/SubwayNodes.csv"))
            using (var subwayNodeCsvReader = new CsvReader(subwayNodeReader, CultureInfo.InvariantCulture))
            using (var railNodeReader = new StreamReader(_path + "/RailNodes.csv"))
            using (var railNodeCsvReader = new CsvReader(railNodeReader, CultureInfo.InvariantCulture))
            using (var lightRailNodeReader = new StreamReader(_path + "/LightRailNodes.csv"))
            using (var lightRailNodeCsvReader = new CsvReader(lightRailNodeReader, CultureInfo.InvariantCulture))
            using (var highwayNodeReader = new StreamReader(_path + "/HighwayNodes.csv"))
            using (var highwayNodeCsvReader = new CsvReader(highwayNodeReader, CultureInfo.InvariantCulture))
            using (var osm2gtfsMapReader = new StreamReader(_path + "/gtfs2osm.csv"))
            using (var osm2gtfsCsvReader = new CsvReader(osm2gtfsMapReader, CultureInfo.InvariantCulture))
            {
                Console.WriteLine("Mapping found! Skipping generation!");
                var tramNodes = tramNodeCsvReader.GetRecords<OsmNode>().ToDictionary(node => node.Id);
                var subwayNodes = subwayNodeCsvReader.GetRecords<OsmNode>().ToDictionary(node => node.Id);
                var railNodes = railNodeCsvReader.GetRecords<OsmNode>().ToDictionary(node => node.Id);
                var lightRailNodes = lightRailNodeCsvReader.GetRecords<OsmNode>().ToDictionary(node => node.Id);
                var highwayNodes = highwayNodeCsvReader.GetRecords<OsmNode>().ToDictionary(node => node.Id);
                var gtfsStopIdDtos = osm2gtfsCsvReader.GetRecords<RawRecord>()
                    .Select(e => e.ToMapping())
                    .ToDictionary(e => e.GtfsId);

                foreach (var stop in _gtfsFeed.Stops)
                {
                    var stopOsmNodes = new List<(OsmType, OsmNode)>();
                    foreach (var (type, id) in gtfsStopIdDtos[stop.Id].OsmId)
                    {
                        switch (type)
                        {
                            case OsmType.TRAM:
                                stopOsmNodes.Add((OsmType.TRAM, tramNodes[id]));
                                break;
                            case OsmType.RAIL:
                                stopOsmNodes.Add((OsmType.RAIL, railNodes[id]));
                                break;
                            case OsmType.SUBWAY:
                                stopOsmNodes.Add((OsmType.SUBWAY, subwayNodes[id]));
                                break;
                            case OsmType.LIGHTRAIL:
                                stopOsmNodes.Add((OsmType.LIGHTRAIL, lightRailNodes[id]));
                                break;
                            default:
                                stopOsmNodes.Add((OsmType.HIGHWAY, highwayNodes[id]));
                                break;
                        }
                    }

                    stop.OsmNode = stopOsmNodes;
                }
            }
        }
        else
        {
            Console.WriteLine("No Mapping Found, Starting generation!");
            using (var tramNodeReader = new StreamReader(_path + "/TramNodes.csv"))
            using (var tramNodeCsv = new CsvReader(tramNodeReader, CultureInfo.InvariantCulture))
            using (var subwayNodeReader = new StreamReader(_path + "/SubwayNodes.csv"))
            using (var subwayNodeCsv = new CsvReader(subwayNodeReader, CultureInfo.InvariantCulture))
            using (var railWayNodeReader = new StreamReader(_path + "/RailNodes.csv"))
            using (var railWayNodeCsv = new CsvReader(railWayNodeReader, CultureInfo.InvariantCulture))
            using (var lightRailNodeReader = new StreamReader(_path + "/LightRailNodes.csv"))
            using (var lightRailNodeCsv = new CsvReader(lightRailNodeReader, CultureInfo.InvariantCulture))
            using (var highwayNodeReader = new StreamReader(_path + "/HighwayNodes.csv"))
            using (var highwayNodeCsv = new CsvReader(highwayNodeReader, CultureInfo.InvariantCulture))
            using (var gtfs2osmWriter = new StreamWriter(_path + "/gtfs2osm.csv"))
            {
                var tramOsmNodes = tramNodeCsv.GetRecords<OsmNode>().ToList();
                var subwayOsmNodes = subwayNodeCsv.GetRecords<OsmNode>().ToList();
                var railOsmNodes = railWayNodeCsv.GetRecords<OsmNode>().ToList();
                var lightRailOsmNodes = lightRailNodeCsv.GetRecords<OsmNode>().ToList();
                var highwayOsmNodes = highwayNodeCsv.GetRecords<OsmNode>().ToList();

                gtfs2osmWriter.WriteLine("gtfsId,osmIds");
                long counter = 0;
                ConcurrentQueue<Osm2GtfsStopIdDTO> entriesToWrite = new ConcurrentQueue<Osm2GtfsStopIdDTO>();
                _tramTree = new OsmKdTree(tramOsmNodes);
                _subwayTree = new OsmKdTree(subwayOsmNodes);
                _railTree = new OsmKdTree(railOsmNodes);
                _lightRailTree = new OsmKdTree(lightRailOsmNodes);
                _highwayTree = new OsmKdTree(highwayOsmNodes);

                foreach (var stop in _gtfsFeed.Stops)
                {
                    if (stop.OsmNode is not null) continue;
                    Parallel.ForEach(_gtfsFeed.Stops,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, stop =>
                        {
                            var matches = new List<(OsmType, OsmNode)>();

                            var tram = _tramTree.FindNearest(stop);
                            if (tram != null) matches.Add((OsmType.TRAM, tram));

                            var subway = _subwayTree.FindNearest(stop);
                            if (subway != null) matches.Add((OsmType.SUBWAY, subway));

                            var rail = _railTree.FindNearest(stop);
                            if (rail != null) matches.Add((OsmType.RAIL, rail));

                            var lightRail = _lightRailTree.FindNearest(stop);
                            if (lightRail != null) matches.Add((OsmType.LIGHTRAIL, lightRail));

                            var highway = _highwayTree.FindNearest(stop);
                            if (highway != null) matches.Add((OsmType.HIGHWAY, highway));

                            stop.OsmNode = matches;
                            entriesToWrite.Enqueue(new Osm2GtfsStopIdDTO()
                            {
                                GtfsId = stop.Id, OsmId = matches.Select(e => (e.Item1, e.Item2.Id)).ToList()
                            });
                        });
                }

                Console.WriteLine("Starting Saving to File!");
                foreach (var entry in entriesToWrite) gtfs2osmWriter.WriteLine(entry.ToString());
                Console.WriteLine("Saved to File!");
            }
        }
    }

    private Dictionary<long, OsmNode> ReadOsmNodes(string filePath)
    {
        Dictionary<long, OsmNode> osmNodes = new Dictionary<long, OsmNode>();

        using (StreamReader r = new StreamReader(filePath))
        using (var csvReader = new CsvReader(r, CultureInfo.InvariantCulture))
        {
            osmNodes = csvReader.GetRecords<OsmNode>().ToDictionary(n => n.Id, n => n);
        }

        return osmNodes;
    }

    private Dictionary<long, OsmWay> ReadOsmEdges(string filePath)
    {
        Dictionary<long, OsmWay> osmEdges = new Dictionary<long, OsmWay>();
        using (StreamReader r = new StreamReader(filePath))
        using (var csvReader = new CsvReader(r, CultureInfo.InvariantCulture))
        {
            osmEdges = csvReader.GetRecords<OsmWay>().ToDictionary(e => e.WayId);
        }

        return osmEdges;
    }

    public void ReadAndSetOsmDataFromFile()
    {
        foreach (OsmType enumValue in System.Enum.GetValues(typeof(OsmType)))
        {
            var (edgeName, nodeName) = GetFileOsmCsvFileNames(enumValue);

            var nodes = ReadOsmNodes(_path + nodeName);
            var ways = ReadOsmEdges(_path + edgeName);

            SetCorrespondingNodeDictionary(enumValue, nodes);
            SetCorrespondingWayDictionary(enumValue, ways);
        }
    }
    
    public void SetGraphs()
    {
        SetStopsToStopTimes();
        SetClosestOsmNodeForGtfsNodes();

        ReadAndSetOsmDataFromFile();

        highwayGraph = OsmGraphFactory.BuildBiderectionalGraph(higwayNodesDict, highwayWays, OsmType.HIGHWAY, _highwayTree);
        Console.WriteLine($"HighwayGraph Size {highwayGraph.EdgeCount}");
        railGraph = OsmGraphFactory.BuildBiderectionalGraph(railNodesDict, railWays, OsmType.RAIL, _railTree);
        Console.WriteLine($"RailGraph Size {railGraph.EdgeCount}");
        tramGraph = OsmGraphFactory.BuildBiderectionalGraph(tramNodesDict, tramWays, OsmType.TRAM, _tramTree);
        Console.WriteLine($"TramGraph Size {tramGraph.EdgeCount}");
        lightRailGraph =
            OsmGraphFactory.BuildBiderectionalGraph(lightRailNodesDict, lightRailWays, OsmType.LIGHTRAIL, _lightRailTree);
        Console.WriteLine($"LightRailGraph Size {lightRailGraph.EdgeCount}");
        subwayGraph = OsmGraphFactory.BuildBiderectionalGraph(subwayNodesDict, subwayWays, OsmType.SUBWAY, _subwayTree);
        Console.WriteLine($"SubwayGraph Size {subwayGraph.EdgeCount}");
    }

    public Dictionary<string, (long Source, long Target)> GetAllBusStopPairs()
    {
        var result = new Dictionary<string, (long, long)>();
        // 1. Collect bus route IDs
        var busRouteIds = _routeDict.Values
            .Where(r => r.RouteType == 3) // BUS
            .Select(r => r.Id)
            .ToHashSet();

        // 2. Iterate over bus trips
        foreach (var trip in _gtfsFeed.Trips)
        {
            if (!busRouteIds.Contains(trip.RouteId))
                continue;

            if (!_stopTimesDict.TryGetValue(trip.Id, out var stopTimes))
                continue;

            var orderedStops = stopTimes
                .OrderBy(st => st.Sequence)
                .Select(st => st.Stop.roadNode.Id)
                .ToList();

            // 3. Extract consecutive stop pairs
            for (int i = 0; i < orderedStops.Count - 1; i++)
            {
                var source = orderedStops[i];
                var target = orderedStops[i + 1];

                // Stable identifier (matches your SubLinks style)
                var key = $"{source} - {target}";

                // Deduplicate across all trips
                if (!result.ContainsKey(key))
                {
                    result[key] = (source, target);
                }
            }
        }

        return result;
    }

    
    public List<List<OsmNode>> ComputeLines(Trip trip)
    {
        var route = _routeDict[trip.RouteId];
        var stopTimes = _stopTimesDict[trip.Id].Select(s =>  s.Stop).ToList();
        var result1 = new List<OsmNode>();
        var result2 = new List<OsmNode>();
        if (route.RouteType == 0)
        {
            result1 = ComputeNodesForLinestring(OsmType.TRAM, stopTimes, null,true)[0];
            result2 = ComputeNodesForLinestring(OsmType.RAIL, stopTimes, null, true)[0];
        }

        if (route.RouteType == 1)
        {
            result1 = ComputeNodesForLinestring(OsmType.SUBWAY, stopTimes, null, true)[0];
        }

        if (route.RouteType == 2)
        {
            result1 = ComputeNodesForLinestring(OsmType.RAIL, stopTimes, null, true)[0];
        }

        if (route.RouteType == 3)
        {
            for (int i = 1; i < stopTimes.Count; i++)
            {
                var tempStopTimes = new[] {stopTimes[i - 1], stopTimes[i]}.ToList();
                var tempResult = ComputeNodesForLinestring(OsmType.HIGHWAY, tempStopTimes, null, true) ??
                                 new List<List<OsmNode>>();
                if (tempResult is null || tempResult.Count == 0 || tempResult[0].Count == 0) continue;
                foreach (var n in tempResult[0] ?? new List<OsmNode>()) result1.Add(n);

            }
        }
        
        return new[] { result1, result2 }.ToList();
    }

    public void ReadSubLinks()
    {
        if (!File.Exists(_path + "/SubLinks.csv")) return;
        using (var reader =  new StreamReader(_path + "/SubLinks.csv"))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records =  csv.GetRecords<SubLinkDto>().ToList();
            this._precomputedShapes = new Dictionary<string, SubLinkDto>();
            foreach (var r in records)
            {
                _precomputedShapes.Add(r.ConstructIdent(), r);
            }
        }
    }

    public void WriteIndivualSegments(Dictionary<string, (Stop source, Stop target)> stops) 
    {
        var existingLines = new List<string>();

        // --- Load existing entries & remove duplicates ---
        if (File.Exists(_path + "/SubLinks.csv"))
        {
            Console.WriteLine("SubLinks already exists");
            using var reader = new StreamReader(_path + "/SubLinks.csv");
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<SubLinkDto>().ToList();
            foreach (var r in records)
            {
                existingLines.Add(r.ToString());
                stops.Remove(r.ConstructIdent());
            }
        }

        // --- Group remaining stops by (source, type) ---
        var grouped = new Dictionary<string, List<Stop>>();

        foreach (var kv in stops)
        {
            var type = kv.Key.Split(new string[] { " - " }, StringSplitOptions.None)[2].ToOsmType();
            
            var key = kv.Value.source.Id + " - " + type;

            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<Stop>();
                grouped[key] = list;
                list.Add(kv.Value.source);
            }

            list.Add(kv.Value.target);
        }

        Console.WriteLine("Creation of subtrip groups finished.");
        Console.WriteLine($"Beginning to process {grouped.Count} subtrip groups.");
        
        var outputLines = new ConcurrentBag<string>();
        int counter = 0;
        Parallel.ForEach(
            grouped,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
            pair =>
            {

                var type = pair.Key.Split(new string[] { " - " }, StringSplitOptions.None)[1].ToOsmType();

                
                var source = pair.Value[0];
                var targets = pair.Value.Skip(1).ToList();
                
                var routes = ComputeNodesForLinestring(
                    type,
                    new List<Stop> { source, targets[0] },
                    targets.Skip(1).ToList());

                for (int i = 0; i < routes.Count; i++)
                {
                    var target = targets[i];
                    var sb = new StringBuilder(256);

                    sb.Append(source.Id).Append(',')
                      .Append(target.Id).Append(',')
                      .Append(type).Append(',');

                    foreach (var n in routes[i])
                    {
                        sb.Append('(')
                          .Append(n.Id).Append(' ')
                          .Append(n.Lat.ToString(CultureInfo.InvariantCulture)).Append(' ')
                          .Append(n.Lon.ToString(CultureInfo.InvariantCulture))
                          .Append(") ");
                    }

                    outputLines.Add(sb.ToString().TrimEnd());
                    counter++;
                    if (counter % 100 == 0) Console.WriteLine($"{counter} / {grouped.Count} subtrips processed.");
                }
            });

        // --- Write everything back (existing + new) ---
        using var writer = new StreamWriter(_path + "/SubLinks.csv");
        writer.WriteLine("source,target,type,coords");

        foreach (var line in existingLines)
            writer.WriteLine(line);

        foreach (var line in outputLines)
            writer.WriteLine(line);
    }

    public List<List<OsmNode>> ComputeNodesForLinestring(
        OsmType type,
        List<Stop> stops,
        List<Stop> additionStopsToCheck = null,
        bool onTheFly = false)
    {

        if (additionStopsToCheck == null && _precomputedShapes is not null && !onTheFly)
        {
            var preResult = new List<List<OsmNode>>();
            var firstResult = new List<OsmNode>();
            preResult.Add(firstResult);
            for (int i = 1; i < stops.Count; i++)
            {
                var keystring = $"{stops[i-1].Id} - {stops[i].Id} - {type}";
                if (_precomputedShapes.ContainsKey(keystring))
                {
                    Console.WriteLine($"{keystring} already exists");
                    var nodes = _precomputedShapes[keystring].SubLinks.Select(s => new OsmNode()
                    {
                        Id = s.OsmId,
                        Lat = s.Lat,
                        Lon = s.Lon,
                    }).ToList();
                    
                    foreach (var n in nodes) firstResult.Add(n);
                }
                else
                {
                    Console.WriteLine("Key does not exist, on the fly computation needed");
                    var tempStopList =  new List<Stop>();
                    tempStopList.Add(stops[i-1]);
                    tempStopList.Add(stops[i]);
                    var nodes = ComputeNodesForLinestring(type, tempStopList, null, true);

                    if (nodes == null || nodes.Count == 0 || nodes[0].Count == 0)
                    {
                        Console.WriteLine($"⚠️ No route found for {stops[i-1].Id} → {stops[i].Id} ({type})");
                        firstResult.Add(stops[i-1].OsmNode.First(n => n.Item1 == type).Item2);
                        firstResult.Add(stops[i].OsmNode.First(n => n.Item1 == type).Item2);
                        continue; // IMPORTANT: do not index nodes[0]
                    }

                    foreach (var n in nodes[0])
                        firstResult.Add(n);

                }
                
            }
            
            return preResult;
        }
        
        additionStopsToCheck ??= new List<Stop>();

        var graph = GetGraph(type);
        var nodeDict = GetNodeDictionary(type);

        var source = stops[0].OsmNode.First(n => n.Item1 == type).Item2.Id;

        var targets = new HashSet<long>
        {
            stops[1].OsmNode.First(n => n.Item1 == type).Item2.Id
        };

        foreach (var s in additionStopsToCheck)
            targets.Add(s.OsmNode.First(n => n.Item1 == type).Item2.Id);

        var remaining = new HashSet<long>(targets);

        var dijkstra = new DijkstraShortestPathAlgorithm<long, OsmEdge>(
            graph,
            e => e.Weight);

        var recorder = new VertexPredecessorRecorderObserver<long, OsmEdge>();

        void OnExamineVertex(long v)
        {
            if (remaining.Remove(v) && remaining.Count == 0)

                    dijkstra.Abort();
                
                
        }

        dijkstra.ExamineVertex += OnExamineVertex;

        try
        {
            using (recorder.Attach(dijkstra))
                dijkstra.Compute(source);
        }
        finally
        {
            dijkstra.ExamineVertex -= OnExamineVertex;
        }

        var result = new List<List<OsmNode>>();

        foreach (var target in targets)
        {
            if (!recorder.TryGetPath(target, out var edges))
                continue;

            var path = new List<OsmNode> { nodeDict[source] };
            long last = source;

            foreach (var e in edges)
            {
                if (e.Target != last)
                    path.Add(nodeDict[e.Target]);
                last = e.Target;
            }

            result.Add(path);
        }

        return result;
    }


    
    public static double DistanceMeters(OsmNode a, Stop b)
    {
        const double R = 6371000.0; // Earth radius in meters
        double lat1 = a.Lat * Math.PI / 180.0;
        double lat2 = b.Lat * Math.PI / 180.0;
        double dLat = lat2 - lat1;
        double dLon = (b.Lon - a.Lon) * Math.PI / 180.0;

        double x = dLon * Math.Cos((lat1 + lat2) / 2.0);
        double y = dLat;
        return Math.Sqrt(x * x + y * y) * R;
    }

    public static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double Distance(OsmNode osmNode, OsmNode node2)
    {
        return Distance(osmNode.Lat, osmNode.Lon, node2.Lat, node2.Lon);
    }

    private (string edgeName, string nodeName) GetFileOsmCsvFileNames(OsmType osmType)
    {
        switch (osmType)
        {
            case OsmType.RAIL:
                return ("/RailWays.csv", "/RailNodes.csv");
            case OsmType.SUBWAY:
                return ("/SubwayWays.csv", "/SubwayNodes.csv");
            case OsmType.LIGHTRAIL:
                return ("/LightRailWays.csv", "/LightRailNodes.csv");
            case OsmType.TRAM:
                return ("/TramWays.csv", "/TramNodes.csv");
            default:
                return ("/HighwayWays.csv", "/HighwayNodes.csv");
        }
    }

    private void SetCorrespondingNodeDictionary(OsmType osmType, Dictionary<long, OsmNode> data)
    {
        switch (osmType)
        {
            case OsmType.RAIL:
                railNodesDict = data;
                break;
            case OsmType.SUBWAY:
                subwayNodesDict = data;
                break;
            case OsmType.LIGHTRAIL:
                lightRailNodesDict = data;
                break;
            case OsmType.TRAM:
                tramNodesDict = data;
                break;
            default:
                higwayNodesDict = data;
                break;
        }
    }

    private void SetCorrespondingWayDictionary(OsmType osmType, Dictionary<long, OsmWay> data)
    {
        switch (osmType)
        {
            case OsmType.RAIL:
                railWays = data;
                break;
            case OsmType.SUBWAY:
                subwayWays = data;
                break;
            case OsmType.LIGHTRAIL:
                lightRailWays = data;
                break;
            case OsmType.TRAM:
                tramWays = data;
                break;
            default:
                highwayWays = data;
                break;
        }
    }

    private BidirectionalGraph<long, OsmEdge> GetGraph(OsmType osmType)
    {
        return osmType switch
        {
            OsmType.RAIL => railGraph,
            OsmType.SUBWAY => subwayGraph,
            OsmType.LIGHTRAIL => lightRailGraph,
            OsmType.TRAM => tramGraph,
            OsmType.HIGHWAY => highwayGraph,
            _ => throw new ArgumentOutOfRangeException(nameof(osmType), osmType, "Unsupported OsmType")
        };
    }

    private Dictionary<long, OsmNode> GetNodeDictionary(OsmType osmType)
    {
        return osmType switch
        {
            OsmType.RAIL => railNodesDict,
            OsmType.SUBWAY => subwayNodesDict,
            OsmType.LIGHTRAIL => lightRailNodesDict,
            OsmType.TRAM => tramNodesDict,
            OsmType.HIGHWAY => higwayNodesDict,
            _ => throw new ArgumentOutOfRangeException(nameof(osmType), osmType, "Unsupported OsmType")
        };
    }
    
    private Dictionary<long, OsmWay> GetWayDictionary(OsmType osmType)
    {
        return osmType switch
        {
            OsmType.RAIL => railWays,
            OsmType.SUBWAY => subwayWays,
            OsmType.LIGHTRAIL => lightRailWays,
            OsmType.TRAM => tramWays,
            OsmType.HIGHWAY => highwayWays,
            _ => throw new ArgumentOutOfRangeException(nameof(osmType), osmType, "Unsupported OsmType")
        };
    }
}
