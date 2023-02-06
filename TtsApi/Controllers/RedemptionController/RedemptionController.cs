using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication;
using TtsApi.Authentication.Policies;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;

namespace TtsApi.Controllers.RedemptionController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanAccessQueue)]
    public class RedemptionController : ControllerBase
    {
        private readonly ILogger<RedemptionController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly TtsSkipHandler _ttsSkipHandler;

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext,
            TtsSkipHandler ttsSkipHandler)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _ttsSkipHandler = ttsSkipHandler;
        }

        /// <summary>
        /// Get all redemptions of a specific channel ordered by RequestTimestamp.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions.
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <returns></returns>
        /// <response code="200">Requested reward.</response>
        /// <response code="404">Channel or reward in channel not found.</response>
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [Produces("application/json")]
        public ActionResult<IEnumerable<RedemptionView>> Get([FromQuery] int roomId)
        {
            IEnumerable<RedemptionView> redemptionViews = _ttsDbContext.Rewards
                .Include(r => r.RequestQueueIngests)
                .Where(r => r.ChannelId == roomId)
                .SelectMany(r => r.RequestQueueIngests)
                .OrderBy(rqi => rqi.RequestTimestamp)
                .Select(rqi => new RedemptionView(rqi));

            return Ok(redemptionViews);
        }

        /// <summary>
        /// Delete a specific redemption in a specific channel. Essentially skipping it.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="redemptionId">RedemptionId of the redemption. Must match roomId. If left empty it will use the first one.</param>
        /// <returns></returns>
        /// <response code="204">Reward successfully skipped.</response>
        /// <response code="404">Channel or reward in Channel not found or nothing to skip.</response>
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<ActionResult> Delete([FromQuery] int roomId, [FromQuery] string redemptionId = null)
        {
            bool successful = string.IsNullOrEmpty(redemptionId)
                ? await _ttsSkipHandler.SkipCurrentTtsRequest(roomId)
                : await _ttsSkipHandler.SkipTtsRequest(roomId, redemptionId);
            return successful ? NoContent() : NotFound();
        }
    }
}
