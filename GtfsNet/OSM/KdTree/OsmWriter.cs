using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace GtfsNet.OSM.KdTree;

public class OsmWriter
{
    
    public static void SaveNodesAsCsv(
        IEnumerable<OsmNode> nodes,
        string outputPath,
        byte decimals)
    {
        var list = nodes.ToList();

        if (list.Count < 1)
            throw new InvalidOperationException("CSV requires at least 1 point");

        // Clamp for safety
        decimals = (byte)Math.Min(decimals, (byte)10);

        string format = "0." + new string('#', decimals);

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("seq,lat,lon");

        for (int i = 0; i < list.Count; i++)
        {
            var n = list[i];

            double lat = Math.Round(n.Lat, decimals);
            double lon = Math.Round(n.Lon, decimals);

            sb.Append(i); // seq
            sb.Append(",");
            sb.Append(lat.ToString(format, CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(lon.ToString(format, CultureInfo.InvariantCulture));
            sb.AppendLine();
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    
    public static void SaveNodesAsGeoJsonLine(
        IEnumerable<OsmNode> nodes,
        string outputPath,
        byte decimals)
    {
        var list = nodes.ToList();

        if (list.Count < 2)
            throw new InvalidOperationException("LineString requires at least 2 points");

        // Clamp for safety (GeoJSON rarely needs > 7)
        decimals = (byte)Math.Min(decimals, (byte)10);

        string format = "0." + new string('#', decimals);

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

            double lon = Math.Round(n.Lon, decimals);
            double lat = Math.Round(n.Lat, decimals);

            sb.Append("          [");
            sb.Append(lon.ToString(format, CultureInfo.InvariantCulture));
            sb.Append(", ");
            sb.Append(lat.ToString(format, CultureInfo.InvariantCulture));
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

    public static void SaveRoutesAsGeoJsonLines(
        IEnumerable<List<OsmNode>> routes,
        string filePath)
    {
        using var writer = new StreamWriter(
            filePath,
            false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) // no BOM
        );

        writer.WriteLine("{");
        writer.WriteLine("  \"type\": \"FeatureCollection\",");
        writer.WriteLine("  \"features\": [");

        bool firstFeature = true;

        foreach (var route in routes)
        {
            if (route == null || route.Count < 2)
                continue;

            if (!firstFeature)
                writer.WriteLine(",");

            firstFeature = false;

            writer.WriteLine("    {");
            writer.WriteLine("      \"type\": \"Feature\",");
            writer.WriteLine("      \"geometry\": {");
            writer.WriteLine("        \"type\": \"LineString\",");
            writer.WriteLine("        \"coordinates\": [");

            for (int i = 0; i < route.Count; i++)
            {
                var n = route[i];

                string lon = n.Lon.ToString(CultureInfo.InvariantCulture);
                string lat = n.Lat.ToString(CultureInfo.InvariantCulture);

                writer.Write("          [");
                writer.Write(lon);
                writer.Write(", ");
                writer.Write(lat);
                writer.Write("]");

                if (i < route.Count - 1)
                    writer.Write(",");

                writer.WriteLine();
            }

            writer.WriteLine("        ]");
            writer.WriteLine("      }");
            writer.WriteLine("    }");
        }

        writer.WriteLine("  ]");
        writer.WriteLine("}");
    }

    public static void SaveRoutesAsGeoJsonLines(
        IEnumerable<List<OsmNode>> routes,
        string filePath, byte decimals)
    {
        using var writer = new StreamWriter(
            filePath,
            false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) // no BOM
        );

        writer.WriteLine("{");
        writer.WriteLine("  \"type\": \"FeatureCollection\",");
        writer.WriteLine("  \"features\": [");

        bool firstFeature = true;

        foreach (var route in routes)
        {
            if (route == null || route.Count < 2)
                continue;

            if (!firstFeature)
                writer.WriteLine(",");

            firstFeature = false;

            writer.WriteLine("    {");
            writer.WriteLine("      \"type\": \"Feature\",");
            writer.WriteLine("      \"geometry\": {");
            writer.WriteLine("        \"type\": \"LineString\",");
            writer.WriteLine("        \"coordinates\": [");

            for (int i = 0; i < route.Count; i++)
            {
                var n = route[i];

                double lon = Math.Round(n.Lon, decimals);
                double lat = Math.Round(n.Lat, decimals);

                writer.Write("          [");
                writer.Write(lon.ToString("0.#####", CultureInfo.InvariantCulture));
                writer.Write(", ");
                writer.Write(lat.ToString("0.#####", CultureInfo.InvariantCulture));
                writer.Write("]");

                if (i < route.Count - 1)
                    writer.Write(",");

                writer.WriteLine();
            }

            writer.WriteLine("        ]");
            writer.WriteLine("      }");
            writer.WriteLine("    }");
        }

        writer.WriteLine("  ]");
        writer.WriteLine("}");
    }

    
}