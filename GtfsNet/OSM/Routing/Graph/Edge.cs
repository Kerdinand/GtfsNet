namespace GtfsNet.Routing
{
    public class Edge
    {
        public float Weight { get; set; } = 0;
        public Node Source {get; set;}
        public Node Target {get; set;}
    }
}