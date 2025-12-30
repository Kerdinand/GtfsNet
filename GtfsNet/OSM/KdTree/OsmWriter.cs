using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GtfsNet.OSM.Rail;

namespace GtfsNet.OSM.KdTree;

public class OsmWriter
{
    
    public static void SaveNodesAsGeoJsonLine(
        IEnumerable<OsmNode> nodes,
        string outputPath)
    {
        var list = nodes.ToList();

        if (list.Count < 2)
            throw new InvalidOperationException("LineString requires at least 2 points");

        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine("  \"type\": \"FeatureCollection\",");
        sb.AppendLine("  \"features\": [");
        sb.AppendLine("    {");
        sb.AppendLine("      \"type\": \"Feature\",");
        sb.AppendLine("      \"geometry\": {");
        sb.AppendLine("        \"type\": \"LineString\",");
        sb.AppendLine("        \"coordinates\": [");

        for (int i = 0; i < list.Count; i++)
        {
            var n = list[i];

            sb.Append("          [");
            sb.Append(n.Lon.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(n.Lat.ToString(CultureInfo.InvariantCulture));
            sb.Append("]");

            if (i < list.Count - 1)
                sb.Append(",");

            sb.AppendLine();
        }

        sb.AppendLine("        ]");
        sb.AppendLine("      },");
        sb.AppendLine("      \"properties\": {}");
        sb.AppendLine("    }");
        sb.AppendLine("  ]");
        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    
    public static void SaveNodesAsGeoJsonPoints(
        IEnumerable<OsmNode> nodes,
        string outputPath)
    {
        var list = nodes.ToList();

        var sb = new StringBuilder();

        sb.AppendLine("{");
        sb.AppendLine("  \"type\": \"FeatureCollection\",");
        sb.AppendLine("  \"features\": [");

        for (int i = 0; i < list.Count; i++)
        {
            var n = list[i];

            sb.AppendLine("    {");
            sb.AppendLine("      \"type\": \"Feature\",");
            sb.AppendLine("      \"geometry\": {");
            sb.AppendLine("        \"type\": \"Point\",");
            sb.Append("        \"coordinates\": [");
            sb.Append(n.Lon.ToString(CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(n.Lat.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("]");
            sb.AppendLine("      },");
            sb.AppendLine("      \"properties\": {");
            sb.Append("        \"node_id\": ");
            sb.Append(n.Id);
            sb.AppendLine();
            sb.AppendLine("      }");
            sb.Append("    }");

            if (i < list.Count - 1)
                sb.Append(",");

            sb.AppendLine();
        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    
    
}