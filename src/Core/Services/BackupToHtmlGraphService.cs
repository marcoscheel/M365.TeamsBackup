using HtmlAgilityPack;
using M365.TeamsBackup.Core.Data;
using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services
{
    public class BackupToHtmlGraphService : IGenericConsoleServiceExecutor
    {
        private readonly ILogger<BackupToHtmlGraphService> _Logger;
        private readonly ILogger<HtmlTeam> _LoggerHtmlTeam;
        private readonly ILogger<HtmlTeamChannel> _LoggerHtmlTeamChannel;
        private readonly ILogger<HtmlTeamChannelMessage> _LoggerHtmlTeamChannelMessage;
        private readonly Config.Html _Options;

        public BackupToHtmlGraphService(ILogger<BackupToHtmlGraphService> logger, ILogger<HtmlTeam> loggerHtmlTeam, ILogger<HtmlTeamChannel> loggerHtmlTeamChannel, ILogger<HtmlTeamChannelMessage> loggerHtmlTeamChannelMessage, IOptions<Config.Html> options)
        {
            _Logger = logger;
            _LoggerHtmlTeam = loggerHtmlTeam;
            _LoggerHtmlTeamChannel = loggerHtmlTeamChannel;
            _LoggerHtmlTeamChannelMessage = loggerHtmlTeamChannelMessage;
            _Options = options.Value;
        }

        public async Task Execute()
        {
            _Logger.LogInformation($"Version: {Core.Config.App.Version}");
            _Logger.LogInformation($"Start Html conversion: {_Options.SourcePath} - {_Options.TargetPath}");

            foreach (var teamPath in System.IO.Directory.EnumerateDirectories(_Options.SourcePath))
            {
                var teamDir = new System.IO.DirectoryInfo(teamPath);
                _Logger.LogInformation($"Team: {teamDir.Name}");

                var htmlTeam = new HtmlTeam(_LoggerHtmlTeam, _Options, teamDir.Name);
                await htmlTeam.Load();
                
                _Logger.LogInformation($"Team: {htmlTeam.Team.Id} - {htmlTeam.Team.DisplayName}");

                foreach (var channelDir in teamDir.EnumerateDirectories())
                {
                    _Logger.LogInformation($"Channel: {channelDir.Name}");

                    var htmlChannel = new HtmlTeamChannel(_LoggerHtmlTeamChannel, _Options, htmlTeam.Team.Id , channelDir.Name);
                    await htmlChannel.Load();

                    _Logger.LogInformation($"Channel: {htmlChannel.Channel.Id} - {htmlChannel.Channel.DisplayName} - {htmlChannel.Channel.MembershipType}");

                    foreach (var messageDir in channelDir.EnumerateDirectories())
                    {
                        _Logger.LogInformation($"Message: {messageDir.Name}");

                        var htmlMessage = new HtmlTeamChannelMessage(_LoggerHtmlTeamChannelMessage, _Options, htmlTeam.Team.Id, htmlChannel.Channel.Id, messageDir.Name);
                        await htmlMessage.Load();
                        _Logger.LogInformation($"Message: {htmlMessage.Message.Id}");

                        var htmlDocument = new HtmlDocument();
                        htmlDocument.Load(_Options.TemplateFile);
                        var bodyNode = htmlDocument.DocumentNode.SelectSingleNode(".//body");
                        
                        await htmlMessage.GetHtml(bodyNode);

                        htmlDocument.Save(HtmlTeamChannelMessage.GetOutputMessageFile(_Options.TargetPath, htmlTeam.Team.Id, htmlChannel.Channel.Id, htmlMessage.Message.Id));
                    }
                }
            }

        }
    }
}
