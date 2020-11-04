using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using CM.Backend.Identity.AuthorizationServer;
using Microsoft.AspNetCore.Mvc;

namespace CM.Backend.API.Controllers
{
    [Route("api/v1/health")]
    public class MonitorController : Controller
    {
        private readonly IUserRepository _userRepository;

        public MonitorController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        
        /// <summary>
        /// Perform a shallow health check
        /// </summary>
        /// <returns></returns>
        [Route("shallow")]
        [HttpGet]
        public async Task<IActionResult> Shallow()
        {
            return Ok();
        }

        /// <summary>
        /// Perform a deep health check, verifying dependent services
        /// </summary>
        /// <returns></returns>
        [Route("deep")]
        [HttpGet]
        public async Task<IActionResult> Deep()
        {
            return await _userRepository.CheckConnection() ? Ok() : StatusCode(500);
        }
    }
}