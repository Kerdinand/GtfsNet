using System.IO;
using GtfsNet.Enum;
using QuikGraph;

namespace GtfsNet.OSM.Graph;

public static class GraphWriter
{
    public static void WriteToFile(this AdjacencyGraph<long, OsmEdge> graph, string path, OsmType type)
    {
        var (nodes, edges) = GetFileName(type);
        using (var writer = new StreamWriter(path + nodes))
        {
            foreach (var line in graph.Vertices)
            {
                
            }
        }
    }

    private static (string nodes, string ways) GetFileName(OsmType type)
    {
        switch (type)
        {
            case OsmType.TRAM:
                return ("/TramGraphNodes.csv", "/TramGraphEdges.csv");
            case OsmType.RAIL:
                return ("/RailGraphNodes.csv", "/RailGraphEdges.csv");
            case OsmType.HIGHWAY:
                return ("/HigwayGraphNodes.csv", "/HigwayGraphEdges.csv");
            case OsmType.SUBWAY:
                return ("/SubwayGraphNodes.csv","/SubwayGraphEdges.csv");
            default:
                return ("/LightRailGraphNodes.csv", "/LightRailGraphEdges.csv");
        }
    }
}