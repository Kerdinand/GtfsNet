namespace GtfsNet.OSM.KdTree;

public class KdNode
{
    public OsmNode Point;
    public KdNode Left;
    public KdNode Right;
    public bool SplitByLat; // true = lat, false = lon

    public KdNode(OsmNode point, bool splitByLat)
    {
        Point = point;
        SplitByLat = splitByLat;
    }
}
