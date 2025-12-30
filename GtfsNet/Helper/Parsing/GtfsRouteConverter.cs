using System.Drawing;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace GtfsNet.Helper.Parsing
{
    public class GtfsByteConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (text == null) return false;
            return byte.Parse(text);
        }
    }
}