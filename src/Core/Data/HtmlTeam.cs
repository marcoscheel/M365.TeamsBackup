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
    public class HtmlTeam
    {
        private readonly ILogger<HtmlTeam> _Logger;
        private readonly Config.Html _Options;
        private readonly string _TeamId;

        public HtmlTeam(ILogger<HtmlTeam> logger, Config.Html options, string teamid)
        {
            _Logger = logger;
            _Options = options;
            _TeamId = teamid;
        }

        private Team _Team;
        public Team Team {
            get
            {
                return _Team;
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
            await LoadTeam();
            await LoadMembers();
        }
        public async Task LoadTeam()
        {
            var jsonFile = MgTeam.GetBackupTeamFile(_Options.SourcePath, _TeamId);
            _Logger.LogTrace($"File: {jsonFile}");

            using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);

            _Team = await JsonSerializer.DeserializeAsync<Team>(fs);
            _Logger.LogTrace($"Load: {_Team.Id} - {_Team.DisplayName}");
        }

        public async Task LoadMembers()
        {
            var jsonFile = MgTeam.GetBackupTeamMembersFile(_Options.SourcePath, _TeamId);
            _Logger.LogTrace($"File: {jsonFile}");

            using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);
            _Members = await JsonSerializer.DeserializeAsync<List<AadUserConversationMember>>(fs);
            _Logger.LogTrace($"Load: {_Team.Id} - {_Team.DisplayName} - {_Members.Count}");
        }

        public async Task Save()
        {
            if (_Team == null)
            {
                await Load();
            }
        }

        public static string GetOutputPath(string root, string teamId)
        {
            var fullpath = System.IO.Path.Combine(root, teamId);
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetOutputTeamFile(string root, string teamId)
        {
            var fullpath = System.IO.Path.Combine(GetOutputPath(root, teamId), "team.html");
            return fullpath;
        }

    }
}
