using GtfsNet.Enum;
using QuikGraph;

namespace GtfsNet.OSM.Graph;

public class OsmEdge : IEdge<long>
{
    public float Weight { get; set; } = 1;
    public long Source { get; }
    public long Target { get; }

    public float penalty { get; set; } = 0f;
    public OsmType OsmType { get; set; }
    
    public OsmEdge(long source, long target, float weight, OsmType osmType)
    {
        Weight = weight;
        Source = source;
        Target = target;
    }
}