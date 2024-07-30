using Microsoft.AspNetCore.Mvc;

namespace SampleAspNetReactDockerApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingSessionController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<string> GetSessionsAsync()
        {
            throw new NotImplementedException();
        }

        [HttpGet("{id}")]
        public string GetSessionAsync(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public void CreateSessionAsync([FromBody] string value)
        {
            throw new NotImplementedException();
        }

        [HttpPut("{id}")]
        public void UpdateSessionAsync(int id, [FromBody] string value)
        {
            throw new NotImplementedException();
        }

        [HttpDelete("{id}")]
        public void CloseSessionAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
