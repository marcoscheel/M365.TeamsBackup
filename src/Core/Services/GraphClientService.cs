using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services
{
    public class GraphClientService : IGraphClientService
    {
        private IPublicClientApplication _PublicClientApplication;
        private readonly Config.AzureAd _Options;
        private GraphServiceClient _GraphClient;

        public GraphClientService(ILogger<GraphClientService> logger, IOptions<Config.AzureAd> options)
        {
            _Options = options.Value;
        }


        public GraphServiceClient GetGraphClient(ILogger logger)
        {
            if (_PublicClientApplication == null)
            {
                _PublicClientApplication = PublicClientApplicationBuilder
                    .Create(_Options.ClientId)
                    .WithAuthority(new Uri($"{_Options.Instance}/{_Options.TenantId}"))
                    .Build();
                TokenCacheHelper.EnableSerialization(_PublicClientApplication.UserTokenCache);

                Func<DeviceCodeResult, Task> deviceCodeReadyCallback = async dcr => await Console.Out.WriteLineAsync(dcr.Message);

                DeviceCodeProvider authProvider = new DeviceCodeProvider(_PublicClientApplication, _Options.Scope, deviceCodeReadyCallback);
                _GraphClient = new GraphServiceClient(authProvider);
            }

            return _GraphClient;
        }
    }
}
