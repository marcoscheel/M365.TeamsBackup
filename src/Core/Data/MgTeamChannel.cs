using M365.TeamsBackup.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Data
{
    public class MgTeamChannel
    {
        private readonly ILogger<MgTeamChannel> _Logger;
        private readonly Config.Backup _Options;
        private readonly IGraphClientService _GraphClientService;
        private readonly string _TeamId;
        private readonly string _ChannelId;
        private bool _SaveMembers = true;
        public MgTeamChannel(ILogger<MgTeamChannel> logger, Config.Backup options, IGraphClientService graphClientService, string teamid, string channelid)
        {
            _Logger = logger;
            _Options = options;
            _GraphClientService = graphClientService;
            _TeamId = teamid;
            _ChannelId = channelid;
        }

        public async Task Save()
        {
            await SaveChannel();
            if (_SaveMembers)
            {
                await SaveMembers();
            }
        }

        public async Task SaveChannel()
        {
            //Request all properties
            var detailRequest = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Request();
            _Logger.LogTrace($"GraphURI: {detailRequest.RequestUrl}");
            var detail = await detailRequest.GetAsync();

            _SaveMembers = detail.MembershipType == ChannelMembershipType.Private;

            var jsonFile = GetBackupChannelFile(_Options.Path, _TeamId, _ChannelId);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"File: {jsonFile}");
            await JsonSerializer.SerializeAsync<Channel>(fs, detail, _Options.JsonOptions);
        }

        public async Task SaveMembers()
        {
            var memberPageRequest = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Members.Request();
            
            var listMember = new List<AadUserConversationMember>();
            do
            {
                _Logger.LogTrace($"MembersGraphURI: {memberPageRequest.RequestUrl}");
                var memberPage = await memberPageRequest.GetAsync();

                foreach (AadUserConversationMember member in memberPage)
                {
                    listMember.Add(member);
                    _Logger.LogTrace($"Member: {member.UserId}|{member.Email}|{member.DisplayName}");
                }
                memberPageRequest = memberPage.NextPageRequest;
            }
            while (memberPageRequest != null);

            var jsonFile = GetBackupChannelMembersFile(_Options.Path, _TeamId, _ChannelId);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"MemberFile: {jsonFile}");
            await JsonSerializer.SerializeAsync<List<AadUserConversationMember>>(fs, listMember, _Options.JsonOptions);
        }

        public static string GetBackupPath(string root, string teamId, string channelId)
        {
            var fullpath = System.IO.Path.Combine(MgTeam.GetBackupPath(root, teamId), channelId.Replace(':', '-').Replace('@', '-'));
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetBackupChannelFile(string root, string teamId, string channelId)
        {
            var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId), "channel.json");
            return fullpath;
        }
        public static string GetBackupChannelMembersFile(string root, string teamId, string channelId)
        {
            var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId), "channel.members.json");
            return fullpath;
        }

    }
}
