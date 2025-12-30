namespace GtfsNet.Routing;

public record Label
{
    float Value {get; set;}
    Node Origin {get; set;}
}