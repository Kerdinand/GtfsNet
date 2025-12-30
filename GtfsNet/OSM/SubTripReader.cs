using System;
using System.Collections.Generic;
using System.Linq;
using GtfsNet.Enum;
using GtfsNet.Structs;

namespace GtfsNet.OSM;

public class SubTripReader
{
    private GtfsFeed _feed;
    private Dictionary<string, List<StopTime>> _stopTimes =  new Dictionary<string, List<StopTime>>();
    private Dictionary<string, Route> _routes = new Dictionary<string, Route>();
    public Dictionary<string, (Stop source, Stop target)> IndividualStops { get; set; }= new Dictionary<string, (Stop source, Stop target)>();
    
    public SubTripReader(GtfsFeed feed)
    {
        _feed = feed;
        _stopTimes = _feed.StopTimesDictionary();
        _routes = _feed.Route.ToDictionary(route => route.Id, route => route);
        if (_feed.StopTimes[0].Stop is null) SetStopsToStopTimes();
    }
    
    private void SetStopsToStopTimes()
    {
        var stopDict = _feed.Stops.ToDictionary(stop => stop.Id, stop => stop);
        foreach (var stopTime in _feed.StopTimes) stopTime.Stop = stopDict[stopTime.StopId]; 
    }

    public Dictionary<string, (Stop source, Stop target)> FindAllSubTrips()
    {
        HashSet<string> alreadyCoveredSubTrips = new HashSet<string>();

        foreach (var trip in _feed.Trips)
        {
            var route = _routes[trip.RouteId];
            var stops =  _stopTimes[trip.Id];

            for (int i = 1; i < stops.Count; i++)
            {
                var ident = ConstructIdentString(stops[i-1].Stop,stops[i].Stop,route);
                if (alreadyCoveredSubTrips.Contains(ident)) continue;
                alreadyCoveredSubTrips.Add(ident);
                IndividualStops.Add(ident, (stops[i-1].Stop, stops[i].Stop));
            }
        }
        
        Console.WriteLine(IndividualStops.Count);
        return IndividualStops;
    }

    private string ConstructIdentString(Stop a, Stop b, Route route)
    {
        return $"{a.Id} - {b.Id} - {route.RouteType}";
    }
    
    
}