using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GtfsNet.OSM.Routing.OsmStreetRouting;

public static class DijkstraManager
{
    private static readonly SemaphoreSlim _semaphore =
        new(Environment.ProcessorCount, Environment.ProcessorCount);

    private static readonly ConcurrentQueue<Dijkstra> _pool = new();
    private static readonly HashSet<byte> _usedIds = new();
    private static readonly object _lock = new();

    public static void Initialize(OsmStreetGraph graph, byte count = 4)
    {
        lock (_lock)
        {
            _pool.Clear();
            _usedIds.Clear();
            // set number of instances

            for (byte id = 0; id < count; id++)
            {
                _usedIds.Add(id);
                var newInstance =  new Dijkstra(id, graph);
                newInstance.RelaxAll();
                _pool.Enqueue(newInstance);
            }

            Console.WriteLine($"Initialized {count} Dijkstra instances");
        }
    }

    public static async Task<List<OsmNode>> RunRouteAsync(
        long source,
        long target)
    {
        await _semaphore.WaitAsync();

        if (!_pool.TryDequeue(out var dijkstra))
            throw new InvalidOperationException("Semaphore out of sync");

        try
        {
            return dijkstra.GetRoute(source, target, 5000);
        }
        finally
        {
            _pool.Enqueue(dijkstra);
            _semaphore.Release();
        }
    }
}