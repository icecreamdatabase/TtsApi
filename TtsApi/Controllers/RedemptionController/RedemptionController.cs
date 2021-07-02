using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication;
using TtsApi.Authentication.Policies;
using TtsApi.Hubs.TtsHub;
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanAccessQueue)]
    public class RedemptionController : ControllerBase
    {
        private readonly ILogger<RedemptionController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;
        private readonly TtsHandler _ttsHandler;

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext,
            IHubContext<TtsHub, ITtsHub> ttsHub, TtsHandler ttsHandler)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _ttsHub = ttsHub;
            _ttsHandler = ttsHandler;
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
        [ProducesResponseType((int) HttpStatusCode.OK)]
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
        /// Delete a specific redemption in a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="redemptionId">Id of the redemption. Must match roomId. If left empty it will use the first one.</param>
        /// <returns></returns>
        /// <response code="204">Reward successfully skipped.</response>
        /// <response code="404">Channel or reward in Channel not found or nothing to skip.</response>
        [HttpDelete]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Delete([FromQuery] int roomId, [FromQuery] string redemptionId = null)
        {
            IIncludableQueryable<RequestQueueIngest, Channel> query = _ttsDbContext.RequestQueueIngest
                .Include(r => r.Reward)
                .Include(r => r.Reward.Channel);

            RequestQueueIngest rqi = string.IsNullOrEmpty(redemptionId)
                //First one
                ? query.FirstOrDefault(r => r.Reward.ChannelId == roomId)
                //Specific one
                : query.FirstOrDefault(r => r.Id.ToString() == redemptionId);

            if (rqi is null || rqi.Reward.ChannelId != roomId)
                return NotFound();

            // Do we need to skip the currently playing one?
            if (TtsHandler.ActiveRequests.TryGetValue(rqi.Reward.ChannelId, out string redId) &&
                redId == rqi.Id.ToString())
            {
                List<string> clients = TtsHandler.ConnectClients
                    .Where(pair => pair.Value == roomId.ToString())
                    .Select(pair => pair.Key)
                    .Distinct()
                    .ToList();
                if (clients.Any())
                    await _ttsHub.Clients.Clients(clients).TtsSkipCurrent();
                else
                    await _ttsHandler.MoveRqiToTtsLog(rqi.Id.ToString(), MessageType.Skipped);
            }
            else
                await _ttsHandler.MoveRqiToTtsLog(rqi.Id.ToString(), MessageType.SkippedBeforePlaying);

            return NoContent();
        }
    }
}
