using System;
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

namespace TtsApi.Controllers.ChannelBlacklistController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeChannelSettings)]
    public class ChannelBlacklistController : ControllerBase
    {
        private readonly ILogger<ChannelBlacklistController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public ChannelBlacklistController(ILogger<ChannelBlacklistController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }


        /// <summary>
        /// Get all blacklisted users in a specific channel.
        /// </summary>
        /// <param name="roomId">Room to get users from.</param>
        /// <returns></returns>
        /// <response code="200">Blacklisted users.</response>
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [Produces("application/json")]
        public ActionResult<IEnumerable<ChannelBlacklistView>> GetChannelBlacklist([FromQuery] int roomId)
        {
            IQueryable<ChannelBlacklistView> bcv = _ttsDbContext.ChannelUserBlacklist
                .Where(cub => cub.ChannelId == roomId && (cub.UntilDate == null || DateTime.Now < cub.UntilDate))
                .Select(cub => new ChannelBlacklistView(cub));
            return Ok(bcv);
        }

        /// <summary>
        /// Adds a specific user to the blacklist of a specific channel.
        /// </summary>
        /// <param name="roomId">Room to add the user in.</param>
        /// <param name="input">User that should be added to the blacklist.</param>
        /// <returns></returns>
        /// <response code="201">User was added to the blacklist.</response>
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult> AddUserToChannelBlacklist([FromQuery] int roomId,
            [FromBody] ChannelBlacklistInput input)
        {
            ChannelUserBlacklist channelUserBlacklist =
                await _ttsDbContext.ChannelUserBlacklist.FindAsync(roomId, input.UserId);
            _ttsDbContext.ChannelUserBlacklist.Remove(channelUserBlacklist);

            ChannelUserBlacklist cub = new ChannelUserBlacklist
            {
                ChannelId = roomId,
                UserId = input.UserId,
                UntilDate = input.UntilDate
            };
            await _ttsDbContext.ChannelUserBlacklist.AddAsync(cub);
            await _ttsDbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetChannelBlacklist), new { roomId }, cub);
        }

        /// <summary>
        /// Removes a specific user from the blacklist of a specific channel.
        /// </summary>
        /// <param name="roomId">Room to delete the user from.</param>
        /// <param name="userId">User to remove from the blacklist.</param>
        /// <returns></returns>
        /// <response code="204">User was removed from the blacklist.</response>
        /// <response code="404">User was not on the blacklist.</response>
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<ActionResult> DeleteUserFromChannelBlacklist([FromQuery] int roomId, [FromQuery] int userId)
        {
            ChannelUserBlacklist cub = await _ttsDbContext.ChannelUserBlacklist.FindAsync(roomId, userId);
            if (cub is null)
                return NotFound();

            _ttsDbContext.ChannelUserBlacklist.Remove(cub);
            await _ttsDbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}
