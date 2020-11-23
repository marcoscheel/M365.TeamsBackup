using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services.Util
{
    public class GenericConsoleService<T> : IHostedService where T : IGenericConsoleServiceExecutor
    {
        private readonly ILogger<T> _Logger;
        private readonly IHostApplicationLifetime _AppLifetime;
        private readonly T _Service;
        public GenericConsoleService(ILogger<T> logger, IHostApplicationLifetime appLifetime, T service)
        {
            _Logger = logger;
            _AppLifetime = appLifetime;
            _Service = service;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation($"Start");
            _AppLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _Service.Execute();
                    }
                    catch (Exception ex)
                    {
                        _Logger.LogError(ex, "Unhandled exception!");
                    }
                    finally
                    {
                        // Stop the application once the work is done
                        _AppLifetime.StopApplication();
                    }
                });
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("Stop");
            return Task.CompletedTask;
        }

    }
}
