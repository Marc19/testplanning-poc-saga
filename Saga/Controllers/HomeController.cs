using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TestPlanningSaga.DTOs;
using TestPlanningSaga.Producers;
using TestPlanningSaga.Messages.Commands;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestPlanningSaga.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IKafkaProducer _kafkaProducer;
        private readonly IConfiguration Configuration;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly string EXPERIMENTS_TOPIC;
        private readonly string METHODS_TOPIC;

        public HomeController(IConfiguration configuration, IKafkaProducer kafkaProducer, ILogger<KafkaProducer> logger)
        {
            _kafkaProducer = kafkaProducer;
            Configuration = configuration;
            EXPERIMENTS_TOPIC = Configuration["ExperimentsTopic"];
            METHODS_TOPIC = Configuration["MethodsTopic"];
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Up and running...");
        }

        [HttpPost]
        public IActionResult CreateExperimentWithMethods(ExperimentWithMethodsDTO experimentWithMethodsDTO)
        {
            long loggedInUserId = GetLoggedInUserIdMockUp();

            if (loggedInUserId == -1)
                return Unauthorized();

            CreateExperiment createExperiment =
                new CreateExperiment(experimentWithMethodsDTO.Experiment.Creator, experimentWithMethodsDTO.Experiment.Name, loggedInUserId);

            List<CreateMethod> createMethods = new List<CreateMethod>();

            foreach (var methodDTO in experimentWithMethodsDTO.Methods)
            {
                CreateMethod createMethod = new CreateMethod(methodDTO.Creator, methodDTO.Name, methodDTO.ApplicationRate, loggedInUserId);
                createMethods.Add(createMethod);
            }

            CreateMethods createMethodsCommand = new CreateMethods(createMethods, loggedInUserId);

            Guid sagaId = Guid.NewGuid();

            StartCreatingExperimentWithMethods startCreatingExperimentWithMethods =
                new StartCreatingExperimentWithMethods(createExperiment, createMethodsCommand, loggedInUserId, sagaId);

            _kafkaProducer.Produce(startCreatingExperimentWithMethods, EXPERIMENTS_TOPIC);

            _logger.LogInformation("In Controller about to return");

            return Ok("Currently processing your request...");
        }

        private long GetLoggedInUserIdMockUp()
        {
            var authorizationHeader = Request.Headers[HeaderNames.Authorization].ToString();
            if (authorizationHeader == "")
                return -1;

            string jwtInput = authorizationHeader.Split(' ')[1];

            var jwtHandler = new JwtSecurityTokenHandler();

            if (!jwtHandler.CanReadToken(jwtInput)) throw new Exception("The token doesn't seem to be in a proper JWT format.");

            var token = jwtHandler.ReadJwtToken(jwtInput);

            var jwtPayload = JsonConvert.SerializeObject(token.Claims.Select(c => new { c.Type, c.Value }));

            JArray rss = JArray.Parse(jwtPayload);
            var firstChild = rss.First;
            var lastChild = firstChild.Last;
            var idString = lastChild.Last.ToString();

            long.TryParse(idString, out long id);

            return id;
        }
    }
}
