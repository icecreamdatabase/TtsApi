using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.ExternalApis.Twitch.Helix;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints.Datatypes;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [ApiController]
    [Route("[controller]/{roomId:int}")]
    [Authorize(Policy = Policies.CanChangeSettings)]
    public class RedemptionController : ControllerBase
    {
        private const string ErrorDuplicateReward = "CREATE_CUSTOM_REWARD_DUPLICATE_REWARD";
        private readonly ILogger<RedemptionController> _logger;
        private readonly TtsDbContext _ttsDbContext;
        private readonly ChannelPoints _channelPoints;

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext,
            ChannelPoints channelPoints)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _channelPoints = channelPoints;
        }

        /// <summary>
        /// Get a specific reward of a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <returns></returns>
        [HttpGet("{rewardId}")]
        public async Task<ActionResult> Get([FromRoute] int roomId, [FromRoute] string rewardId)
        {
            Reward dbReward = _ttsDbContext.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
            if (dbReward?.ChannelId == roomId)
                return Ok((RedemptionRewardView) dbReward);
            return NotFound();
        }

        /// <summary>
        /// Get all rewards of a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions</param>
        /// <returns></returns>
        [HttpGet("")]
        public async Task<ActionResult> GetAll([FromRoute] int roomId)
        {
            List<Reward> dbRewards = _ttsDbContext.Rewards.Where(r => r.ChannelId == roomId).ToList();
            List<RedemptionRewardView> rewardViews = dbRewards.Select(r => (RedemptionRewardView) r).ToList();
            return Ok(rewardViews);
        }

        /// <summary>
        /// Create a new reward for a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions</param>
        /// <returns></returns>
        [HttpPost("")]
        public async Task<ActionResult> Create([FromRoute] int roomId, [FromForm] RedemptionCreateInput input)
        {
            Channel channel = _ttsDbContext.Channels.FirstOrDefault(c => c.RoomId == roomId);
            if (channel is null)
                return NotFound();

            TwitchCustomRewardInput twitchInput = new()
            {
                Title = input.Title,
                Prompt = input.Prompt,
                Cost = input.Cost,
            };

            DataHolder<TwitchCustomReward> dataHolder =
                await _channelPoints.CreateCustomReward(channel, twitchInput);

            if (dataHolder.Data is {Count: > 0})
            {
                TwitchCustomReward reward = dataHolder.Data.First();
                if (reward?.Id is null)
                    return Problem(null, null, (int) HttpStatusCode.ServiceUnavailable);
                Reward newReward = new()
                {
                    RewardId = reward.Id,
                    ChannelId = int.Parse(reward.BroadcasterId),
                    VoiceId = input.VoiceId
                };
                _ttsDbContext.Rewards.Add(newReward);
                await _ttsDbContext.SaveChangesAsync();

                return Created($"{@Url.Action("Get")}/{reward.Id}", (RedemptionRewardView) newReward);
            }

            return dataHolder is {Status: (int) HttpStatusCode.BadRequest, Message: ErrorDuplicateReward}
                ? BadRequest("Title already exists")
                : Problem(dataHolder.Message, null, (int) HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Update settings of a specific reward in a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <returns></returns>
        [HttpPatch("{rewardId}")]
        public async Task<ActionResult> Update([FromRoute] int roomId, [FromRoute] string rewardId)
        {
            return Ok();
        }

        /// <summary>
        /// Delete a specific reward in a specific channel.
        /// </summary>
        /// <param name="roomId">Id of the channel. Must match auth permissions</param>
        /// <param name="rewardId">Id of the reward. Must match roomId.</param>
        /// <returns></returns>
        [HttpDelete("{rewardId}")]
        public async Task<ActionResult> Delete([FromRoute] int roomId, [FromRoute] string rewardId)
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
