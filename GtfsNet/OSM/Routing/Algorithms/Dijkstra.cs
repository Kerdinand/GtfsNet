using System.Collections.Generic;

namespace GtfsNet.Routing;

public class Dijkstra
{
    Dictionary<long, Node> nodes = new Dictionary<long, Node>();
    Dictionary<long, Edge> edges = new Dictionary<long, Edge>();

    public Dijkstra()
    {
        
    }
}