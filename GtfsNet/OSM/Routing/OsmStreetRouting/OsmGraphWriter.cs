using System.Globalization;
using System.IO;
using CsvHelper;

namespace GtfsNet.OSM.Routing.OsmStreetRouting;

public class OsmGraphWriter
{
    private string _path;
    
    public OsmGraphWriter(string path)
    {
        _path = path;
    }

    public void WriteGraphEdges(OsmStreetGraph graph)
    {
        using (var writer = new StreamWriter(_path + "/OsmGraphEdges.csv"))
        using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            //Writer.WriteRecords(graph.Edges);
        }
    }
}