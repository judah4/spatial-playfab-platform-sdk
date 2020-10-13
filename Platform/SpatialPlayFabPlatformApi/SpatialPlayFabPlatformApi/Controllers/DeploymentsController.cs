using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Improbable.SpatialOS.Deployment.V1Alpha1;
using Improbable.SpatialOS.PlayerAuth.V2Alpha1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;

namespace SpatialPlayFabPlatformApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeploymentsController : ControllerBase
    {

        private readonly ILogger<DeploymentsController> _log;
        private IConfiguration _configuration;


        private readonly DeploymentServiceClient _deploymentServiceClient;
        private readonly PlayerAuthServiceClient _playerAuthServiceClient;

        public DeploymentsController(ILogger<DeploymentsController> log, IConfiguration configuration)
        {
            _log = log;
            _configuration = configuration;

            PlayFabSettings.staticSettings.TitleId = _configuration["PlayFab:TitleId"];
            PlayFabSettings.staticSettings.DeveloperSecretKey = _configuration["PlayFab:SecretKey"];

            var credentialsWithProvidedToken = SpatialAccountService.GetPlatformRefreshToken();
            _deploymentServiceClient = DeploymentServiceClient.Create(credentials: credentialsWithProvidedToken);
            _playerAuthServiceClient = PlayerAuthServiceClient.Create(credentials: credentialsWithProvidedToken);
        }

        [NonAction]
        public async Task<UserAccountInfo> AuthorizedAccessAsync()
        {

            StringValues authValue;

            Request.Headers.TryGetValue("Authorization", out authValue);

            if (authValue.Count < 1)
            {
                throw new Exception("Not authorized");
            }
            string token = authValue[0].Split(' ').Last();

            var authed = await PlayFabServerAPI.AuthenticateSessionTicketAsync(new AuthenticateSessionTicketRequest()
            { SessionTicket = token });

            if (authed.Error != null)
            {
                throw new Exception(authed.Error.GenerateErrorReport());
            }

            return authed.Result.UserInfo;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                StringValues authValue;

                Request.Headers.TryGetValue("Authorization", out authValue);

                if (authValue.Count < 1)
                {
                    return new ContentResult()
                    {
                        Content = JsonConvert.SerializeObject(new { Error = $"Not Authorized" }),
                        ContentType = "application/json",
                        StatusCode = 400
                    };

                }

                var creds = await AuthorizedAccessAsync();

                var deployCreds = new List<dynamic>();

                var spatialProject = _configuration["SpatialOS:Project"];

                //spatial
                //get this and store it somewhere in the future. for now, just keep calling
                var playerIdentityTokenResponse = _playerAuthServiceClient.CreatePlayerIdentityToken(
                    new CreatePlayerIdentityTokenRequest
                    {
                        Provider = "provider",

                        PlayerIdentifier = creds.PlayFabId,
                        ProjectName = spatialProject,
                    });


                var playerAccessDeploymentTag = _configuration["SpatialOS:DeploymentTag"];

                var deployments = _deploymentServiceClient.ListDeployments(new ListDeploymentsRequest
                {
                    ProjectName = spatialProject,

                }).Where(d => d.Status == Deployment.Types.Status.Running && d.Tag.Contains(playerAccessDeploymentTag)).ToList();

                foreach (var deployment in deployments)
                {
                    var createLoginTokenResponse = _playerAuthServiceClient.CreateLoginToken(
                        new CreateLoginTokenRequest
                        {
                            PlayerIdentityToken = playerIdentityTokenResponse.PlayerIdentityToken,
                            DeploymentId = deployment.Id,
                            LifetimeDuration = Duration.FromTimeSpan(new TimeSpan(0, 0, 30, 0)),
                            WorkerType = "UnityClient" //change this if you are using

                        });

                    deployCreds.Add(new
                    {
                        DeploymentName = deployment.Name,
                        LoginToken = createLoginTokenResponse.LoginToken,
                        Tags = string.Join(",", deployment.Tag),

                    });
                }

                var content = new { SpatialDeployments = deployCreds, PlayerIdentityToken = playerIdentityTokenResponse.PlayerIdentityToken };
                return new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(content),
                    ContentType = "application/json",
                };

            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message);

                //var ravenClient = Logger<>.CreateLogger();
                //ravenClient.Capture(new SentryEvent(e));
                //log error
                return new ContentResult()
                {
                    Content = JsonConvert.SerializeObject(new { Error = $"Something went wrong." }),
                    ContentType = "application/json",
                    StatusCode = 500
                };
            }

        }
    }
}
