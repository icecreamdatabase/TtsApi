using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.ExternalApis.Twitch.Helix;
using TtsApi.ExternalApis.Twitch.Helix.ChannelPoints;
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

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext,
            ChannelPoints channelPoints)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
            _channelPoints = channelPoints;
        }

        [HttpGet("Get/{rewardId}")]
        public async Task<ActionResult> Get([FromRoute] string rewardId)
        {
            Reward dbReward = _ttsDbContext.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
            return Ok((RedemptionRewardView) dbReward);
        }

        [HttpGet("GetAll/{roomId}")]
        public async Task<ActionResult> GetAll([FromRoute] string roomId)
        {
            List<Reward> dbRewards = _ttsDbContext.Rewards.Where(r => r.ChannelId == int.Parse(roomId)).ToList();
            List<RedemptionRewardView> rewardViews = dbRewards.Select(r => (RedemptionRewardView) r).ToList();
            return Ok(rewardViews);
        }

        [HttpPost("Create/{roomId}")]
        public async Task<ActionResult> Create([FromRoute] string roomId, [FromForm] RedemptionCreateInput input)
        {
            Channel channel = _ttsDbContext.Channels.FirstOrDefault(c => c.RoomId == int.Parse(roomId));
            if (channel is null)
                return NotFound();

            TwitchCustomRewardInput twitchInput = new()
            {
                Title = input.Title,
                Prompt = input.Prompt,
                Cost = input.Cost,
            };

            DataHolder<TwitchCustomReward> dataHolder =
                await _channelPoints.CreateCustomReward(roomId, channel, twitchInput);

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

        [HttpPatch("Update/{rewardId}")]
        public async Task<ActionResult> Update([FromRoute] string rewardId)
        {
            return Ok();
        }

        [HttpDelete("Delete/{rewardId}")]
        public async Task<ActionResult> Delete([FromRoute] string rewardId)
        {
            return NoContent();
        }
    }
}
