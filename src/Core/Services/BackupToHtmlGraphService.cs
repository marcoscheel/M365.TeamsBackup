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

                var htmlTeamDocument = new HtmlDocument();

                var htmlTeam = new HtmlTeam(_LoggerHtmlTeam, _Options, teamDir.Name);
                await htmlTeam.Load();
                
                var teamHead = await htmlTeam.GetHtml(htmlTeamDocument);


                _Logger.LogInformation($"Team: {htmlTeam.Team.Id} - {htmlTeam.Team.DisplayName}");

                foreach (var channelDir in teamDir.EnumerateDirectories())
                {
                    _Logger.LogInformation($"Channel: {channelDir.Name}");

                    var htmlChannel = new HtmlTeamChannel(_LoggerHtmlTeamChannel, _Options, htmlTeam.Team.Id , channelDir.Name);
                    await htmlChannel.Load();

                    var htmlChannelDocument = new HtmlDocument();
                    htmlChannelDocument.Load(_Options.TemplateFile);

                    var channelBodyNode = htmlChannelDocument.DocumentNode.SelectSingleNode(".//body");
                    channelBodyNode.AppendChild(teamHead);

                    var channelHead = await htmlChannel.GetHtml(htmlChannelDocument);
                    channelBodyNode.AppendChild(channelHead);

                    _Logger.LogInformation($"Channel: {htmlChannel.Channel.Id} - {htmlChannel.Channel.DisplayName} - {htmlChannel.Channel.MembershipType}");

                    foreach (var messageDir in channelDir.EnumerateDirectories().OrderBy(d => Convert.ToInt64(d.Name))) //Just a hack! Load all messages and reply and oder by last reply of a thread
                    {
                        _Logger.LogInformation($"Message: {messageDir.Name}");

                        var htmlMessage = new HtmlTeamChannelMessage(_LoggerHtmlTeamChannelMessage, _Options, htmlTeam.Team.Id, htmlChannel.Channel.Id, messageDir.Name);
                        await htmlMessage.Load();
                        _Logger.LogInformation($"Message: {htmlMessage.Message.Id}");

                        var htmlMessageDocument = new HtmlDocument();
                        htmlMessageDocument.Load(_Options.TemplateFile);
                        var messageBodyNode = htmlMessageDocument.DocumentNode.SelectSingleNode(".//body");
                        
                        var thread = await htmlMessage.GetHtml(htmlMessageDocument);
                        messageBodyNode.AppendChild(thread);

                        if (_Options.CreateSingleHtmlForMessage)
                        {
                            htmlMessageDocument.Save(HtmlTeamChannelMessage.GetOutputMessageFile(_Options.TargetPath, htmlTeam.Team.Id, htmlChannel.Channel.Id, htmlMessage.Message.Id));
                        }
                        //add to channel
                        channelBodyNode.AppendChild(thread);
                    }
                    htmlChannelDocument.Save(HtmlTeamChannel.GetOutputChannelFile(_Options.TargetPath, htmlTeam.Team.Id, htmlChannel.Channel.Id));
                }


            }

        }
    }
}
