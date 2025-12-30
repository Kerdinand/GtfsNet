using System;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace GtfsNet.Helper.Parsing
{
    public class GtfsDateConverter: DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (text == null) return default(DateTime);

            var year = int.Parse(text.Substring(0,4));
            var month = byte.Parse(text.Substring(4,2));
            var day = byte.Parse(text.Substring(6,2));
            
            return new DateTime(year, month, day);
        }
    }
    
    public class GtfsBoolConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (text == null) return false;
            return text == "1";
        }
    }
}