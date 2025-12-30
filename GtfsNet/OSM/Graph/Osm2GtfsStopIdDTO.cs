using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Enum;
using GtfsNet.Helper.Parsing;
using GtfsNet.OSM.Rail;

namespace GtfsNet.OSM.Graph;

public class Osm2GtfsStopIdDTO
{
    [Ignore]
    public List<(OsmType, long)> OsmId { get; set; }
    [Name("gtfsId")]
    public string GtfsId { get; set; }
    

    public override string ToString()
    {

        var stringToPrint = "";

        foreach (var osmId in OsmId) stringToPrint += $"({osmId.Item1}:{osmId.Item2})";
        
        return $"{GtfsId},{stringToPrint}";
    }
}

public class RawRecord
{
    [Name("gtfsId")]
    public string GtfsId { get; set; }

    [Name("osmIds")]
    public string OsmIdsRaw { get; set; }
    
    private static readonly Regex OsmRegex =
        new(@"\((?<type>[A-Z]+):(?<id>\d+)\)", RegexOptions.Compiled);

    public Osm2GtfsStopIdDTO ToMapping()
    {
        return ToMapping(OsmIdsRaw);
    }
    
    public Osm2GtfsStopIdDTO ToMapping(string raw)
    {
        var mapping = new Osm2GtfsStopIdDTO()
        {
            GtfsId = GtfsId,
        };
        
        var Osmids = OsmRegex.Matches(raw).Cast<Match>().Select<Match, (OsmType, long)>(m =>
        
            (ParseOsmType(m.Groups["type"].Value), long.Parse(m.Groups["id"].Value))
        ).ToList();

        mapping.OsmId = Osmids;
        return mapping;
    }
    
    static OsmType ParseOsmType(string s) => s switch
    {
        "TRAM" => OsmType.TRAM,
        "SUBWAY" => OsmType.SUBWAY,
        "RAIL" => OsmType.RAIL,
        "LIGHTRAIL" => OsmType.LIGHTRAIL,
        "HIGHWAY" => OsmType.HIGHWAY,
        _ => throw new FormatException($"Unknown OsmType '{s}'")
    };
}