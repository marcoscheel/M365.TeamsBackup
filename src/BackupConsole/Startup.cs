using M365.TeamsBackup.Core.Services;
using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace M365.TeamsBackup.BackupConsole
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Core.Config.Backup>(Configuration.GetSection("M365").GetSection("Backup"));
            services.Configure<Core.Config.AzureAd>(Configuration.GetSection("AzureAd"));

            //services.AddTransient<DemoService>();
            services.AddTransient<BackupFromGraphService>();
            services.AddSingleton<IGraphClientService, GraphClientService>();

            //services.AddHostedService<GenericConsoleService<DemoService>>();
            services.AddHostedService<GenericConsoleService<BackupFromGraphService>>();

        }
    }
}
