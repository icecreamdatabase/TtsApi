using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication;
using TtsApi.Authentication.Policies;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.ChannelController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeSettings)]
    public class ChannelController : ControllerBase
    {
        private readonly ILogger<ChannelController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public ChannelController(ILogger<ChannelController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        /// <summary>
        /// Get all settings of a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions.
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <returns></returns>
        /// <response code="200">Requested channel.</response>
        /// <response code="404">Channel not found.</response>
        [HttpGet]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [Produces("application/json")]
        public async Task<ActionResult<ChannelView>> Get([FromQuery] int roomId)
        {
            Channel dbChannel = await _ttsDbContext.Channels.FindAsync(roomId);

            if (dbChannel is null || !dbChannel.Enabled)
                return NotFound();

            return Ok(new ChannelView(dbChannel));
        }

        /// <summary>
        /// Update settings of a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <returns></returns>
        /// <response code="204">Channel updated successfully or nothing was changed.</response>
        /// <response code="404">Channel not found.</response>
        [HttpPatch]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Update([FromQuery] int roomId, [FromForm] ChannelUpdateInput input)
        {
            Channel dbChannel = await _ttsDbContext.Channels.FindAsync(roomId);

            if (dbChannel is null || !dbChannel.Enabled)
                return NotFound();

            try
            {
                bool dbChanged = false;
                foreach (var pi in input.GetType().GetProperties())
                {
                    if (pi.GetValue(input) is null) continue;

                    PropertyInfo piDb = dbChannel.GetType().GetProperty(pi.Name);
                    piDb?.SetValue(dbChannel, pi.GetValue(input));
                    dbChanged = true;
                }

                if (dbChanged)
                    await _ttsDbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception e)
            {
                _logger.LogError("{E}", e);
                return Problem("Saving data failed.", null, (int) HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Deactivate a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <returns></returns>
        /// <response code="204">Channel successfully deactivated.</response>
        /// <response code="404">Channel not found.</response>
        [HttpDelete]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Delete([FromQuery] int roomId)
        {
            return Problem(null, null, (int) HttpStatusCode.NotImplemented);
            
            Channel dbChannel = await _ttsDbContext.Channels.FindAsync(roomId);
            if (dbChannel is null || !dbChannel.Enabled)
                return NotFound();
            dbChannel.Enabled = false;
            await _ttsDbContext.SaveChangesAsync();
            //TODO: revoke all token of said user
            return NoContent();
        }
    }
}
