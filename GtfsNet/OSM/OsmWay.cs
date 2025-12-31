using System;
using System.Linq;
using CsvHelper.Configuration.Attributes;

namespace GtfsNet.OSM.Graph;

public class OsmWay
{
    [Name("way_id")]
    public long WayId {get; set;}
    
    [Name("node_ids")]
    public string NodeIds
    {
        get => _nodeIds;
        set
        {
            _nodeIds = value;
            _nodeIdArray = null; // invalidate cache
        }
    }

    public long[] NodeIdArray
    {
        get
        {
            if (_nodeIdArray == null)
            {
                _nodeIdArray = _nodeIds
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(long.Parse)
                    .ToArray();
            }

            return _nodeIdArray;
        }
    }

    [Name("tags")]
    public string Tags {get; set;}
    
    private string _nodeIds;
    
    private long[]? _nodeIdArray;

    
    
}