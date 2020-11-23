using M365.TeamsBackup.Core.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace M365.TeamsBackup.BackupConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
