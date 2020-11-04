using System.Collections.Generic;

namespace CM.Backend.Identity.AuthorizationServer.Middleware.IPMiddleware
{
    public class IPStackResponseModel
    {
        public string Ip { get; set; }
        public string Type { get; set; }
        public string Continent_code { get; set; }
        public string Continent_name { get; set; }
        public string Country_code { get; set; }
        public string Country_name { get; set; }
        public string Region_code { get; set; }
        public string Region_name { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Location Location { get; set; }
    }
    
    public class Language
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Native { get; set; }
    }

    public class Location
    {
        public int Geoname_id { get; set; }
        public string Capital { get; set; }
        public List<Language> Languages { get; set; }
        public string Country_flag { get; set; }
        public string Country_flag_emoji { get; set; }
        public string Country_flag_emoji_unicode { get; set; }
        public string Calling_code { get; set; }
        public bool Is_eu { get; set; }
    }
}