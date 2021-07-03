using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication;
using TtsApi.Authentication.Policies;
using TtsApi.ExternalApis.Twitch.Helix;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.DataTypes;
using TtsApi.Hubs.TtsHub;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RewardController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeSettings)]
    public class RewardController : ControllerBase
    {
        private const string ErrorDuplicateReward = "CREATE_CUSTOM_REWARD_DUPLICATE_REWARD";
        private readonly ILogger<RewardController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly ChannelPoints _channelPoints;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;

        public RewardController(ILogger<RewardController> logger, TtsDbContext ttsDbContext,
            ChannelPoints channelPoints, IHubContext<TtsHub, ITtsHub> ttsHub)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _channelPoints = channelPoints;
            _ttsHub = ttsHub;
        }

        /// <summary>
        /// Get all or a specific reward of a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions.
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <returns></returns>
        /// <response code="200">Requested reward.</response>
        /// <response code="404">Channel or reward in channel not found.</response>
        [HttpGet]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [Produces("application/json")]
        public async Task<ActionResult<RewardView>> Get([FromQuery] int roomId, [FromQuery] string rewardId)
        {
            Channel inputChannel;
            Reward inputReward = null;
            if (!string.IsNullOrEmpty(rewardId))
            {
                inputReward = _ttsDbContext.Rewards
                    .Include(r => r.Channel)
                    .FirstOrDefault(r => r.RewardId == rewardId);
                if (inputReward?.ChannelId != roomId)
                    return NotFound();
                inputChannel = inputReward.Channel;
            }
            else
            {
                inputChannel = _ttsDbContext.Channels.FirstOrDefault(channel => channel.RoomId == roomId);
            }

            DataHolder<TwitchCustomReward> dataHolder = await _channelPoints.GetCustomReward(inputChannel, inputReward);

            List<RewardView> rewardViews = dataHolder.Data
                .Select(twitchCustomReward =>
                {
                    inputReward ??= _ttsDbContext.Rewards.FirstOrDefault(r => r.RewardId == twitchCustomReward.Id);
                    return new RewardView(inputReward, twitchCustomReward);
                })
                .ToList();

            return Ok(rewardViews);
        }

        /// <summary>
        /// Create a new reward for a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <response code="201">Created reward.</response>
        /// <response code="404">Channel or reward in channel not found.</response>
        /// <response code="400">Title already exists.</response>
        [HttpPost]
        [ProducesResponseType((int) HttpStatusCode.Created)]
        [Produces("application/json")]
        public async Task<ActionResult<RewardView>> Create([FromQuery] int roomId,
            [FromBody] RewardCreateInput input)
        {
            Channel channel = _ttsDbContext.Channels.FirstOrDefault(c => c.RoomId == roomId);
            if (channel is null)
                return NotFound();

            TwitchCustomRewardInputCreate twitchInput = new()
            {
                Title = input.Title,
                Prompt = input.Prompt,
                Cost = input.Cost,
                IsEnabled = true,
                IsUserInputRequired = true,
                ShouldRedemptionsSkipRequestQueue = false
            };

            DataHolder<TwitchCustomReward> dataHolder =
                await _channelPoints.CreateCustomReward(channel, twitchInput);

            if (dataHolder.Data is {Count: > 0})
            {
                TwitchCustomReward twitchCustomReward = dataHolder.Data.First();
                if (twitchCustomReward?.Id is null)
                    return Problem(null, null, (int) HttpStatusCode.ServiceUnavailable);
                Reward newReward = new()
                {
                    RewardId = twitchCustomReward.Id,
                    ChannelId = int.Parse(twitchCustomReward.BroadcasterId),
                    VoiceId = input.VoiceId
                };
                _ttsDbContext.Rewards.Add(newReward);
                await _ttsDbContext.SaveChangesAsync();

                return Created($"{@Url.Action("Get")}/{twitchCustomReward.Id}",
                    new RewardView(newReward, twitchCustomReward));
            }

            return dataHolder is {Status: (int) HttpStatusCode.BadRequest, Message: ErrorDuplicateReward}
                ? BadRequest("Title already exists")
                : Problem(dataHolder.Message, null, (int) HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Update settings of a specific reward in a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <response code="204">Reward updated successfully or nothing was changed..</response>
        /// <response code="404">Channel or reward in channel not found.</response>
        /// <response code="400">Title already exists.</response>
        [HttpPatch]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Update([FromQuery] int roomId, [FromQuery] string rewardId,
            [FromBody] RewardUpdateInput input)
        {
            Reward dbReward = _ttsDbContext.Rewards
                .Include(r => r.Channel)
                .FirstOrDefault(r => r.RewardId == rewardId);

            if (dbReward is null)
                return NoContent();
            if (dbReward.ChannelId != roomId)
                return NotFound();

            if (input.VoiceId is not null)
            {
                dbReward.VoiceId = input.VoiceId;
                await _ttsDbContext.SaveChangesAsync();
                input.VoiceId = null;
            }

            bool allPropertiesAreNull = input.GetType().GetProperties() //get all properties on object
                .Select(pi => pi.GetValue(input)) //get value for the property
                .All(value => value == null); // Check if one of the values is not null, if so it returns true.

            if (allPropertiesAreNull)
                return NoContent();

            // This always needs to be true. No matter what.
            if (input.IsUserInputRequired is not null)
                input.IsUserInputRequired = true;

            DataHolder<TwitchCustomReward> dataHolder = await _channelPoints.UpdateCustomReward(dbReward, input);
            if (dataHolder.Data is {Count: > 0})
            {
                TwitchCustomReward reward = dataHolder.Data.First();
                return reward?.Id is null
                    ? Problem(null, null, (int) HttpStatusCode.ServiceUnavailable)
                    : NoContent();
                //TODO: duplicate title
            }

            return Problem(dataHolder.Message, null, (int) HttpStatusCode.ServiceUnavailable);
        }

        /// <summary>
        /// Delete a specific reward in a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <returns></returns>
        /// <response code="204">Reward successfully deleted.</response>
        /// <response code="404">Channel or reward in Channel not found.</response>
        [HttpDelete]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<ActionResult> Delete([FromQuery] int roomId, [FromQuery] string rewardId)
        {
            Reward dbReward = _ttsDbContext.Rewards
                .Include(r => r.Channel)
                .FirstOrDefault(r => r.RewardId == rewardId);

            if (dbReward is null)
                return NoContent();
            if (dbReward.ChannelId != roomId)
                return NotFound();

            if (await _channelPoints.DeleteCustomReward(dbReward))
            {
                _ttsDbContext.Rewards.Remove(dbReward);
                await _ttsDbContext.SaveChangesAsync();
                return NoContent();
            }

            return Problem(null, null, (int) HttpStatusCode.ServiceUnavailable);
        }
    }
}
