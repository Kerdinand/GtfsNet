using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper.Configuration.Attributes;

namespace GtfsNet.OSM;

public class SubLinkDto
{
    [Name("source")]
    public string Source { get; set; }
    [Name("target")]
    public string Target { get; set; }
    [Name("type")]
    public string Type { get; set; }
    [Name("coords")]
    public string Coords { get; set; }
    [Ignore]
    private List<SubLinkDtoCoord> _subLinks;
    [Ignore]
    public List<SubLinkDtoCoord> SubLinks
    {
        get
        {
            if (_subLinks == null)
            {
                _subLinks = new List<SubLinkDtoCoord>();
                var matches = Coords.Split(new [] { ") (" }, StringSplitOptions.None);
                foreach (var match in matches)
                { 
                    var clean = match.Trim('(', ')', ' ');
                    var parts = clean.Split(' ');
                     _subLinks.Add(new SubLinkDtoCoord()
                     {
                         OsmId = long.Parse(parts[0]),
                         Lat = double.Parse(parts[1]),
                         Lon = double.Parse(parts[2]),
                     });   
                }

            }
            return _subLinks;
        }
    }

    public override string ToString()
    {
        return $"{Source},{Target},{Type},{Coords}";
    }

    public string ConstructIdent()
    {
        return $"{Source} - {Target} - {Type}";
        
    }
}

public class SubLinkDtoCoord
{
    public long OsmId { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
}