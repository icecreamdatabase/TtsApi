using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Roles;
using TtsApi.Hubs.TtsHub;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = Roles.BotOwner)]
    public class ManagementController: ControllerBase
    {
        private readonly ILogger<ManagementController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;

        public ManagementController(ILogger<ManagementController> logger, TtsDbContext ttsDbContext, IHubContext<TtsHub, ITtsHub> ttsHub)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _ttsHub = ttsHub;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            return Ok();
        }

        [HttpPost("Reload")]
        public async Task<ActionResult> Post()
        {
            await _ttsHub.Clients.All.Reload();
            return NoContent();
        }

        [HttpGet("GetDb")]
        [Authorize(Roles = Roles.BotOwner)]
        public ActionResult GetDb()
        {
            _ttsDbContext.Database.EnsureCreated();
            return Ok(_ttsDbContext.Database.GenerateCreateScript());
        }
    }
}
