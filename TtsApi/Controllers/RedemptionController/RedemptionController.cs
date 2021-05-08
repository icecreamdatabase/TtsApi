using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Model;
using TtsApi.Model.Schema;

namespace TtsApi.Controllers.RedemptionController
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.CanChangeSettings)]
    public class RedemptionController : ControllerBase
    {
        private readonly ILogger<RedemptionController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public RedemptionController(ILogger<RedemptionController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        [HttpGet("Get/{rewardId}")]
        public async Task<ActionResult> Get([FromRoute] string rewardId)
        {
            Reward dbReward = _ttsDbContext.Rewards.FirstOrDefault(r => r.RewardId == rewardId);
            return Ok(dbReward);
        }

        [HttpGet("GetAll/{roomId}")]
        public async Task<ActionResult> GetAll([FromRoute] string roomId)
        {
            List<Reward> dbRewards = _ttsDbContext.Rewards.Where(r => r.ChannelId == int.Parse(roomId)).ToList();
            return Ok(dbRewards);
        }

        [HttpPost("Create/{roomId}")]
        public async Task<ActionResult> Create([FromRoute] string roomId, [FromForm] RedemptionCreateInput input)
        {
            //return Created();
            return Ok();
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
