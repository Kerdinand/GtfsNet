using System;
using System.Collections.Generic;
using GtfsNet.OSM.Rail;

namespace GtfsNet.Routing
{
    public class Node
    {
        public Dictionary<byte, Label> Labels {get; set;} = new Dictionary<byte, Label>();
        public List<Edge> Edges = new List<Edge>();
        public long OsmId {get; set;}
        public double Lat {get; set;}
        public double Lon {get; set;}
    }
}