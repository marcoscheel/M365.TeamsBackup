using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Data
{
    public class HtmlTeamChannel
    {
        private readonly ILogger<HtmlTeamChannel> _Logger;
        private readonly Config.Html _Options;
        private readonly string _TeamId;
        private readonly string _ChannelId;

        public HtmlTeamChannel(ILogger<HtmlTeamChannel> logger, Config.Html options, string teamid, string channelid)
        {
            _Logger = logger;
            _Options = options;
            _TeamId = teamid;
            _ChannelId = channelid;
        }

        private Channel _Channel;
        public Channel Channel
        {
            get
            {
                return _Channel;
            }
        }
        private List<AadUserConversationMember> _Members;
        public List<AadUserConversationMember> Members
        {
            get
            {
                return _Members;
            }
        }
        public async Task Load()
        {
            await LoadChannel();
            await LoadMembers();
        }

        public async Task LoadChannel()
        {
            var jsonFile = MgTeamChannel.GetBackupChannelFile(_Options.SourcePath, _TeamId, _ChannelId);
            _Logger.LogTrace($"File: {jsonFile}");

            using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);

            _Channel = await JsonSerializer.DeserializeAsync<Channel>(fs);
        }
        public async Task LoadMembers()
        {
            var jsonFile = MgTeamChannel.GetBackupChannelMembersFile(_Options.SourcePath, _TeamId, _ChannelId);
            _Logger.LogTrace($"File: {jsonFile}");

            if (System.IO.File.Exists(jsonFile))
            {
                using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);

                _Members = await JsonSerializer.DeserializeAsync<List<AadUserConversationMember>> (fs);
            }
        }

        public async Task Save()
        {
            if (_Channel == null)
            {
                await LoadChannel();
            }
            if (_Members == null)
            {
                await LoadMembers();
            }

            //TODO: Save
        }

        public static string GetOutputPath(string root, string teamId, string channelId)
        {
            var fullpath = System.IO.Path.Combine(HtmlTeam.GetOutputPath(root, teamId), channelId.Replace(':', '-').Replace('@', '-'));
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetOutputChannelFile(string root, string teamId, string channelId)
        {
            var fullpath = System.IO.Path.Combine(GetOutputPath(root, teamId, channelId), "channel.html");
            return fullpath;
        }

    }
}
