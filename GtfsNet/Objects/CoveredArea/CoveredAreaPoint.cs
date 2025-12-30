namespace GtfsNet.Objects.CoveredArea
{
    public class CoveredAreaPoint
    {
        public double Lat;
        public double Lon;
        public float Transparency;

        public CoveredAreaPoint(double lat, double lon, float transparency)
        {
            Lat = lat;
            Lon = lon;
            this.Transparency = transparency;
        }

        public override string ToString()
        {
            return $"{Lat},{Lon},{Transparency}";
        }
    }
}