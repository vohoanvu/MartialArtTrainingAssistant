using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleAspNetReactDockerApp.Server.Helpers;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        public VideoController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpPost("/metadata/{videoId}")]
        [Authorize]
        public async Task<VideoDetailsResponse> GetVideoMetadata(string videoId)
        {
            throw new NotImplementedException();
        }
    }
}
