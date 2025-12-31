using System.Collections.Generic;
using System.Linq;

namespace GtfsNet.OSM.KdTree;

public class NearestHeap
{
    private readonly int _capacity;
    private readonly List<(OsmNode node, double dist)> _items = new();

    public NearestHeap(int capacity)
    {
        _capacity = capacity;
    }

    public double WorstDistance =>
        _items.Count < _capacity
            ? double.MaxValue
            : _items.Max(e => e.dist);

    public void TryAdd(OsmNode node, double dist)
    {
        if (_items.Count < _capacity)
        {
            _items.Add((node, dist));
            return;
        }

        int worstIndex = 0;
        double worst = _items[0].dist;

        for (int i = 1; i < _items.Count; i++)
        {
            if (_items[i].dist > worst)
            {
                worst = _items[i].dist;
                worstIndex = i;
            }
        }

        if (dist < worst)
            _items[worstIndex] = (node, dist);
    }

    public List<OsmNode> ToSortedList()
    {
        return _items
            .OrderBy(e => e.dist)
            .Select(e => e.node)
            .ToList();
    }
}
