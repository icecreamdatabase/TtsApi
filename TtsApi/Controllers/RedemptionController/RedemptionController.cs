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
using TtsApi.Hubs.TtsHub.TransformationClasses;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeSettings)]
    public class RedemptionController : ControllerBase
    {
        private const string ErrorDuplicateReward = "CREATE_CUSTOM_REWARD_DUPLICATE_REWARD";
        private readonly ILogger<RedemptionController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly ChannelPoints _channelPoints;
        private readonly IHubContext<TtsHub, ITtsHub> _ttsHub;

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext,
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
        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int roomId, [FromQuery] string rewardId)
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

            List<RedemptionRewardView> redemptionRewardViews = dataHolder.Data
                .Select(twitchCustomReward =>
                {
                    inputReward ??= _ttsDbContext.Rewards.FirstOrDefault(r => r.RewardId == twitchCustomReward.Id);
                    return new RedemptionRewardView(inputReward, twitchCustomReward);
                })
                .ToList();

            return Ok(redemptionRewardViews);
        }

        /// <summary>
        /// Create a new reward for a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Create([FromQuery] int roomId, [FromForm] RedemptionCreateInput input)
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
                    new RedemptionRewardView(newReward, twitchCustomReward));
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
        [HttpPatch]
        public async Task<ActionResult> Update([FromQuery] int roomId, [FromQuery] string rewardId,
            [FromForm] RedemptionUpdateInput input)
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
        [HttpDelete]
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

        /// <summary>
        /// Skips the currently playing redemption. 
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions
        ///     Parameter name defined by <see cref="ApiKeyAuthenticationHandler.RoomIdQueryStringName"/>.</param>
        [HttpPost("Skip")]
        [Authorize(Policy = Policies.CanAccessQueue)]
        public async Task<ActionResult> Skip([FromQuery] int roomId)
        {
            bool hasReward = await _ttsDbContext.Rewards.AnyAsync(reward => reward.ChannelId == roomId);

            if (!hasReward) return NotFound();

            List<string> clients = TtsHandler.ConnectClients
                .Where(pair => pair.Value == roomId.ToString())
                .Select(pair => pair.Key)
                .Distinct()
                .ToList();
            if (clients.Any())
            {
                await _ttsHub.Clients.Clients(clients).TtsSkipCurrent();
            }

            return NoContent();
        }
    }
}
