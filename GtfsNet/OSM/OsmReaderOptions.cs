using System.Collections.Generic;
using GtfsNet.Enum;
using OsmSharp;

namespace GtfsNet.OSM;

public class OsmReaderOptions
{
    public List<OsmType> AllowedOsmTypes { get; set; }
    public List<OsmType> ForbiddenOsmTypes { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    
}