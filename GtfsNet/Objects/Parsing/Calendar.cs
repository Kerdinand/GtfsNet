using System;
using CsvHelper.Configuration.Attributes;
using GtfsNet.Helper.Parsing;

namespace GtfsNet.Structs
{
    public class Calendar
    {
        [Name("service_id")]
        public string ServiceId { get; set; }
        [Name("monday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Monday { get; set; }
        [Name("tuesday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Tuesday { get; set; }
        [Name("wednesday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Wednesday { get; set; }
        [Name("thursday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Thursday { get; set; }
        [Name("friday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Friday { get; set; }
        [Name("saturday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Saturday { get; set; }
        [Name("sunday")]
        [TypeConverter(typeof(GtfsBoolConverter))]
        public bool Sunday { get; set; }
        [Name("start_date")]
        [TypeConverter(typeof(GtfsDateConverter))]
        public DateTime StartDate { get; set; }
        [Name("end_date")]
        [TypeConverter(typeof(GtfsDateConverter))]
        public DateTime EndDate { get; set; }

        public bool IsInTimeFrame(DateTime dateTime)
        {
            return dateTime.Date >= StartDate && dateTime.Date <= EndDate;
        }
        
        public bool GetEntryForDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return Monday;
                case DayOfWeek.Tuesday:
                    return Tuesday;
                case DayOfWeek.Wednesday:
                    return Wednesday;
                case DayOfWeek.Thursday:
                    return Thursday;
                case DayOfWeek.Friday:
                    return Friday;
                case DayOfWeek.Saturday:
                    return Saturday;
                default:
                    return Sunday;
            }
        }
    }
}