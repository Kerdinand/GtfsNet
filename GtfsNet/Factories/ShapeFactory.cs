using System.Collections.Generic;

namespace GtfsNet.Factories;

public class ShapeFactory
{
    private GtfsFeed _gtfsFeed;

    public ShapeFactory(GtfsFeed gtfsFeed)
    {
        _gtfsFeed = gtfsFeed;
    }
    
}