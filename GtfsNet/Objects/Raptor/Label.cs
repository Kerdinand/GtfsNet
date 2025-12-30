using System;

namespace GtfsNet.Objects.Raptor;

public class Label
{
    public DateTime ArrivalTime { get; set; } = DateTime.MaxValue;
    public DateTime DepartureTime { get; set; } =  DateTime.MaxValue;
    public TimeTable ArrivedFrom { get; set; }
    public TimeTableTrip ArrivedWithTrip { get; set; }
    public TimeTable DepartureTrip { get; set; }
}