using System;
using GtfsNet.Enum;
using GtfsNet.OSM;

namespace GtfsNet.Helper.Parsing;

public static class OsmTupleHelper
{
    public static string ToCSVString(this (OsmType type, OsmNode node) input)
    {
        return $"({input.type}:{input.node.Id})";
    }
    
    public static double DistanceTo(this OsmNode a, OsmNode b)
    {
        const double R = 6371000.0; // Earth radius in meters

        double lat1 = a.Lat * Math.PI / 180.0;
        double lat2 = b.Lat * Math.PI / 180.0;
        double dLat = lat2 - lat1;
        double dLon = (b.Lon - a.Lon) * Math.PI / 180.0;

        double x = dLon * Math.Cos((lat1 + lat2) * 0.5);
        double y = dLat;

        return Math.Sqrt(x * x + y * y) * R;
    }

    public static OsmType ToOsmType(this string type)
    {
        switch (type)
        {
            case "0":
            case "TRAM":
                return OsmType.TRAM;
            case "2":
            case "RAIL":
                return OsmType.RAIL;
            case "HIGHWAY":
            case "3":
                return OsmType.HIGHWAY;
            default:
                return OsmType.SUBWAY;
        }
    }
}