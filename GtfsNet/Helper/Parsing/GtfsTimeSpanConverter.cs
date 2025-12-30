using System;
using System.Linq;

namespace GtfsNet.Helper.Parsing
{
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;

    public class GtfsTimeSpanConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(
            string text,
            IReaderRow row,
            MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default(TimeSpan);

            var temp = text.Split(':').Select(entry => ushort.Parse(entry)).ToArray();
            var days = temp[0] / 24;
            var hours = temp[0] % 24;
            var minutes = temp[1];
            var seconds = temp[2];

            return new TimeSpan(days,hours, minutes, seconds);
        }
    }

}