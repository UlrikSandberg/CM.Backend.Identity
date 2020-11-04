namespace CM.Backend.Identity.AuthorizationServer.Middleware.IPMiddleware
{
    public class IPGeolocationModel
    {
        public string IP { get; set; }
        public string Continent_Code { get; set; }
        public string Country_Code { get; set; }
        public string Region_Code { get; set; }
        public int GeonameId { get; set; }
        public string City { get; set; }
        
        public double[] Location { get; set; }
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public void SetLocationArr()
        {
            Location = new double[2];
            Location[0] = Longitude;
            Location[1] = Latitude;
        }
    }
}