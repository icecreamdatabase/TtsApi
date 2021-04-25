using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Roles;
using TtsApi.Hubs;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub> _hubContext;

        public TestController(ILogger<TestController> logger, TtsDbContext ttsDbContext, IHubContext<TtsHub> hubContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _hubContext = hubContext;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult Get()
        {
            // Triggering IndexOutOfRangeException on purpose
            int[] a = new int[1];
            return Ok(a[2]);
        }

        [HttpGet("{channelId}")]
        [Authorize(Policy = Policies.CanAccessQueue)]
        public async Task<ActionResult> Get([FromRoute] string channelId)
        {
            await TtsHub.SendToChannel(_hubContext, channelId, "Pog");
            return Ok($"xD {channelId}");
        }

        [HttpGet("GetDb")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult GetDb()
        {
            _ttsDbContext.Database.EnsureCreated();
            return Ok(_ttsDbContext.Database.GenerateCreateScript());
        }
    }
}
