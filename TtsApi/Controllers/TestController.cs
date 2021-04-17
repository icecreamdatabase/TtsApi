using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        private readonly TtsDbContext _ttsDbContext;

        [HttpGet]
        [Authorize(Policy = Policies.Admin)]
        public string Get()
        {
            //_ttsDbContext.Database.EnsureCreated();
            //return _ttsDbContext.Database.GenerateCreateScript();
            return "xd";
        }
    }
}
