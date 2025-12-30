using System;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Helper.Parsing;

namespace GtfsNet.Structs
{
    public class CalendarDate
    {
        [Name("service_id")]
        public string ServiceId { get; set; }
        [Name("date")]
        [TypeConverter(typeof(GtfsDateConverter))]
        public DateTime Date { get; set; }
        [Name("exception_type")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool ExceptionType { get; set; }
    }
}