using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TtsApi.Authentication.Policies;
using TtsApi.Authentication.Roles;
using TtsApi.Model;

namespace TtsApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly TtsDbContext _ttsDbContext;

        public TestController(ILogger<TestController> logger, TtsDbContext ttsDbContext)
        {
            _logger = logger;
            _ttsDbContext = ttsDbContext;
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult Get()
        {
            throw new Exception("xD");
            return Ok("xD");
        }

        [HttpGet("{channelId}")]
        [Authorize(Policy = Policies.CanAccessQueue)]
        public ActionResult Get([FromRoute] string channelId)
        {
            return Ok($"xD {channelId}");
        }

        [HttpGet("GetDb")]
        [Authorize(Roles = Roles.Admin)]
        public ActionResult GetDb()
        {
            _ttsDbContext.Database.EnsureCreated();
            return Ok(_ttsDbContext.Database.GenerateCreateScript());
        }
    }
}
