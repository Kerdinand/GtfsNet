using System;
using System.Collections.Generic;

namespace GtfsNet.Factories
{
    public class GtfsBaseFactory
    {
        public GtfsBaseFactory(GtfsFeed gtfsFeed)
        {
            _gtfsFeed = gtfsFeed;
        }

        private GtfsFeed _gtfsFeed;
    }
}