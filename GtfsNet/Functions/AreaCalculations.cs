using System.Collections.Generic;
using System.Linq;
using GtfsNet.Objects.CoveredArea;
using GtfsNet.Structs;

namespace GtfsNet.Functions
{
    public class AreaCalculations
    {
        private GtfsFeed _gtfsFeed;

        private Dictionary<string, List<StopTime>> _areaPoints;
        public Dictionary<string, List<StopTime>> AreaPoints
        {
            get
            {
                if (_areaPoints == null)
                {
                    _areaPoints = new Dictionary<string, List<StopTime>>();
                    SetStopTimeDictionary();
                }
                return _areaPoints;
            }
        }

        private int? _maxNumberOfDepartures = null;

        public int MaxNumberOfDepartures
        {
            get
            {
                if (_maxNumberOfDepartures == null) _maxNumberOfDepartures = AreaPoints.Max(_ => _.Value.Count);
                return _maxNumberOfDepartures.Value;
            }
        }

        public AreaCalculations(GtfsFeed gtfsFeed)
        {
            _gtfsFeed = gtfsFeed;
        }

        private List<CoveredAreaPoint> _coveredAreaPoints = null;

        public List<CoveredAreaPoint> CoveredAreaPoints
        {
            get
            {
                if (_coveredAreaPoints == null) _coveredAreaPoints = CalculateCoveredArea();
                return _coveredAreaPoints;
            }
        }
        
        public List<CoveredAreaPoint> CalculateCoveredArea()
        {
            var result = new List<CoveredAreaPoint>();
            foreach (var entry in _gtfsFeed.Stops)
            {
                if (!AreaPoints.ContainsKey(entry.Id)) continue;
                var location = new CoveredAreaPoint(entry.Lat, entry.Lon, AreaPoints[entry.Id].Count);
                result.Add(location);
            }
            return result;
        }

        public List<(double lat, double lon)> CalculateHull()
        {
            var allPoints = new List<(double lat, double lon)>();
            foreach (var entry in CoveredAreaPoints)
            {
                allPoints.Add((entry.Lat, entry.Lon));
            }
            return allPoints;
        }

        private void SetStopTimeDictionary()
        {
            foreach (var entry in _gtfsFeed.StopTimes)
            {
                if (!_areaPoints.ContainsKey(entry.StopId)) _areaPoints.Add(entry.StopId, new List<StopTime>());
                _areaPoints[entry.StopId].Add(entry);
            }

            foreach (var entry in _areaPoints.Values)
            {
                entry.Sort((a, b) => a.ArrivalTime.CompareTo(b.ArrivalTime));
            }
        }
    }
}