using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Context;
using ILogger = Serilog.ILogger;

namespace CM.Backend.Identity.AuthorizationServer.Middleware.IPMiddleware
{
    public class IPLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IOptions<IPStackConfiguration> _ipStackConfig;
        private readonly IMemoryCache _memoryCache;
        private const string NullIP = "::1";


        public IPLoggingMiddleware(RequestDelegate next, ILogger logger, IOptions<IPStackConfiguration> ipStackConfig,
            IMemoryCache memoryCache)
        {
            _next = next;
            _logger = logger;
            _ipStackConfig = ipStackConfig;
            _memoryCache = memoryCache;
        }

        private async Task<IPStackResponseModel> GetIPInfo(string remoteIp)
        {
            return await _memoryCache.GetOrCreateAsync(remoteIp, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(1);
                
                _logger.Information("IP country unknown and not present in cache, performing external lookup...");
                
                var ipStackBaseUrl =
                    $"{_ipStackConfig.Value.BaseUrl}/{remoteIp}?access_key={_ipStackConfig.Value.API_Key}";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(ipStackBaseUrl);
                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsAsync<IPStackResponseModel>();

                    _logger.Error("Got error from IPStack: {HttpStatus}/{ErrorMessage}", response.StatusCode,
                        response.ReasonPhrase);
                    return null;
                }
            });
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                //Read cloudflare connection headers - https://support.cloudflare.com/hc/en-us/articles/200170986-How-does-Cloudflare-handle-HTTP-Request-headers-
                context.Request.Headers.TryGetValue("Cf-Connecting-IP", out var cfConnectingIp);
                context.Request.Headers.TryGetValue("Cf-Ipcountry", out var cfIpCountry);
                context.Request.Headers.TryGetValue("Cf-Ray", out var cfRay);

                var remoteIp = cfConnectingIp.FirstOrDefault() ?? context.Connection.RemoteIpAddress.ToString();

                LogContext.PushProperty("ClientIp", remoteIp);
                LogContext.PushProperty("CfIpCountry", cfIpCountry.FirstOrDefault());
                LogContext.PushProperty("CfRay", cfRay.FirstOrDefault());

                //TODO: Consider if/when to add this in again. Response time seems to be affected, so that should be investigated
//                if (!remoteIp.Equals(NullIP))
//                {
//                    var ipInfo = await GetIPInfo(remoteIp);
//
//                    if (ipInfo != null)
//                    {
//                        var geoModel = GenericMapper<IPStackResponseModel, IPGeolocationModel>.Map(
//                            new MapperConfiguration(
//                                cfg =>
//                                {
//                                    cfg.CreateMap<IPStackResponseModel, IPGeolocationModel>()
//                                        .ForMember(x => x.GeonameId,
//                                            opt => opt.MapFrom(src => src.Location.Geoname_id));
//                                }), ipInfo);
//
//                        geoModel.SetLocationArr();
//
//                        _logger.Information("Request received from {IP}, GeoDetails: {@GeoModel}, {Location}",
//                            context.Connection.RemoteIpAddress.ToString(), geoModel, geoModel.Location);
//                    }
//                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error looking up countrycode from IP from external service. Continuing processing request..");
                
                //Continue processing - this is not (that) critical
            }

            await _next.Invoke(context);
        }
    }
    
    public static class GenericMapper<TSource, TDestination>
    {          
        public static TDestination Map(TSource source)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TSource, TDestination>();
            });

            IMapper mapper = config.CreateMapper();

            return mapper.Map<TSource, TDestination>(source);         
        } 

        public static TDestination Map(TSource source, TDestination destination)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TSource, TDestination>();
            });

            IMapper mapper = config.CreateMapper();

            mapper.Map<TSource, TDestination>(source, destination);
            
            return destination;
        }

        public static TDestination Map(MapperConfiguration customMap, TSource source)
        {
            var config = customMap;
            config.AssertConfigurationIsValid();

            IMapper mapper = config.CreateMapper();

            return mapper.Map<TSource, TDestination>(source);
        }
    }

}