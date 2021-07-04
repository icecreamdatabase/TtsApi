using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.GlobalBlacklistController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeBotSettings)]
    public class GlobalBlacklistController : ControllerBase
    {
        private readonly ILogger<GlobalBlacklistController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public GlobalBlacklistController(ILogger<GlobalBlacklistController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        /// <summary>
        /// Get all globally blacklisted users.
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Blacklisted users.</response>
        [HttpGet]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [Produces("application/json")]
        public ActionResult<IEnumerable<globalBlacklistView>> GetGlobal()
        {
            return Ok(_ttsDbContext.GlobalUserBlacklist.Select(gub => new globalBlacklistView(gub)));
        }

        /// <summary>
        /// Adds a specific user to the global blacklist.
        /// </summary>
        /// <param name="input">User that should be added to the blacklist</param>
        /// <returns></returns>
        /// <response code="201">User was added to the blacklist.</response>
        [HttpPost]
        [ProducesResponseType((int) HttpStatusCode.Created)]
        public async Task<ActionResult> AddGlobal([FromBody] GlobalBlacklistInput input)
        {
            GlobalUserBlacklist gub = new GlobalUserBlacklist {UserId = input.UserId};
            await _ttsDbContext.GlobalUserBlacklist.AddAsync(gub);
            await _ttsDbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGlobal), null, gub);
        }

        /// <summary>
        /// Removes a specific user from the global blacklist.
        /// </summary>
        /// <param name="userId">User to remove from the blacklist</param>
        /// <returns></returns>
        /// <response code="204">User was removed from the blacklist.</response>
        /// <response code="404">User was not on the blacklist.</response>
        [HttpDelete]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> DeleteGlobal([FromQuery] int userId)
        {
            GlobalUserBlacklist gub = await _ttsDbContext.GlobalUserBlacklist.FindAsync(userId);
            if (gub is null)
                return NotFound();

            _ttsDbContext.GlobalUserBlacklist.Remove(gub);
            await _ttsDbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
