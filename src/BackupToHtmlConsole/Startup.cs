using M365.TeamsBackup.Core.Services;
using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.BackupToHtmlConsole
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
            services.Configure<Core.Config.Html>(Configuration.GetSection("M365").GetSection("Html"));

            //services.AddTransient<DemoService>();
            services.AddTransient<BackupToHtmlGraphService>();

            //services.AddHostedService<GenericConsoleService<DemoService>>();
            services.AddHostedService<GenericConsoleService<BackupToHtmlGraphService>>();

        }
    }
}
