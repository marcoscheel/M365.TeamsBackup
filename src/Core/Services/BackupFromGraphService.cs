using M365.TeamsBackup.Core.Data;
using M365.TeamsBackup.Core.Services.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Services
{
    public class BackupFromGraphService : IGenericConsoleServiceExecutor
    {
        private readonly ILogger<BackupFromGraphService> _Logger;
        private readonly ILogger<MgTeam> _LoggerMgTeam;
        private readonly ILogger<MgTeamChannel> _LoggerMgChannel;
        private readonly Config.Backup _Options;
        private readonly IGraphClientService _GraphClientService;

        public BackupFromGraphService(ILogger<BackupFromGraphService> logger, ILogger<MgTeam> loggerMgTeam, ILogger<MgTeamChannel> loggerMgChannel, IOptions<Config.Backup> options, IGraphClientService graphClientService)
        {
            _Logger = logger;
            _LoggerMgTeam = loggerMgTeam;
            _LoggerMgChannel = loggerMgChannel;
            _Options = options.Value;
            _GraphClientService = graphClientService;
        }

        public async Task Execute()
        {
            _Logger.LogInformation($"Version: {Core.Config.App.Version}");
            _Logger.LogInformation($"Start backup at folder: {_Options.Path}");


            //Get teams
            var teamsPageRequestBase = 
                string.IsNullOrEmpty(_Options.TeamId)
                ? _GraphClientService.GetGraphClient(_Logger).Me.JoinedTeams.Request()
                : _GraphClientService.GetGraphClient(_Logger).Me.JoinedTeams.Request().Filter($"Id eq '{_Options.TeamId}'");

            var teamsPageRequest = teamsPageRequestBase
                    .Select(g => new
                    {
                        g.Id,
                        g.DisplayName
                    });



            do { 
                _Logger.LogTrace($"GraphUri: {teamsPageRequest.RequestUrl}");
                var teamsPage = await teamsPageRequest.GetAsync();

                //Process all selected teams
                foreach (var team in teamsPage)
                {
                    _Logger.LogInformation($"Team: {team.Id} - {team.DisplayName}");

                    //Save team
                    var mgteam = new Data.MgTeam(_LoggerMgTeam, _Options, _GraphClientService, team.Id);
                    await mgteam.Save();

                    //Get all channels
                    var channelPageRequest = _GraphClientService.GetGraphClient(_Logger).Teams[team.Id].Channels
                        .Request()
                        .Select(g => new
                        {
                            g.Id,
                            g.DisplayName,
                            g.MembershipType
                        });
                    do
                    {
                        _Logger.LogTrace($"ChannelGraphUri: {channelPageRequest.RequestUrl}");
                        var channelPage = await channelPageRequest.GetAsync();
                        
                        //Process all channels
                        foreach (var channel in channelPage)
                        {
                            _Logger.LogInformation($"Channel: {channel.Id} - {channel.DisplayName} - {channel.MembershipType}");

                            //Save channel
                            var mgchannel = new Data.MgTeamChannel(_LoggerMgChannel, _Options, _GraphClientService, team.Id, channel.Id);
                            await mgchannel.Save();

                            //Get all channel messages
                            //if the current principal is not part of the private channel (only delegate permission) the get channel messages will fail if the user is not part of the channel!
                            //for now I go with a try/catch
                            try
                            {
                                var messagePageRequest = _GraphClientService.GetGraphClient(_Logger).Teams[team.Id].Channels[channel.Id].Messages.Request();

                                do
                                {
                                    _Logger.LogTrace($"MessageGraphUri: {messagePageRequest.RequestUrl}");
                                    var messagePage = await messagePageRequest.GetAsync();

                                    foreach (var message in messagePage)
                                    {
                                        _Logger.LogDebug($"Message: {message.Id} - {message.Subject} - {message.Summary}");

                                        var mgmessage = new Data.MgTeamChannelMessage(_LoggerMgChannel, _Options, _GraphClientService, team.Id, channel.Id, message.Id);
                                        if (message.DeletedDateTime != null)
                                        {
                                            await mgmessage.SaveChatMessage(message);
                                        }
                                        else
                                        {
                                            await mgmessage.Save(message);
                                        }
                                    }
                                    messagePageRequest = messagePage.NextPageRequest;
                                }
                                while (messagePageRequest != null) ;
                            }
                            catch (Microsoft.Graph.ServiceException messageEx)
                            {
                                if (channel.MembershipType == Microsoft.Graph.ChannelMembershipType.Private && messageEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                                {
                                    _Logger.LogWarning($"Error while accessing messages in private channel: {channel.Id} - {channel.DisplayName} - {channel.MembershipType} - {messageEx.StatusCode}");
                                }
                                else
                                {
                                    _Logger.LogError(messageEx, $"Error while gettings messages from channel: {channel.Id} - {channel.DisplayName} - {channel.MembershipType} - {messageEx.StatusCode}");
                                }
                            }
                        }
                        channelPageRequest = channelPage.NextPageRequest;
                    }
                    while (channelPageRequest != null);
                }
                teamsPageRequest = teamsPage.NextPageRequest;
            }
            while (teamsPageRequest != null);
        }
    }
}
