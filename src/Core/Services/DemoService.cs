using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services
{
    public class DemoService : IGenericConsoleServiceExecutor
    {
        private readonly ILogger<DemoService> _Logger;
        private readonly Config.Backup _Options;

        public DemoService(ILogger<DemoService> logger, IOptions<Config.Backup> options)
        {
            _Logger = logger;
            _Options = options.Value;
        }

        public async Task Execute()
        {
            _Logger.LogInformation("Weclome to Demo Service");
        }
    }
}
