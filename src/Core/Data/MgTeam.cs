using M365.TeamsBackup.Core.Services;
using M365.TeamsBackup.Core.Services.Util;
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
    public class MgTeam
    {
        private readonly ILogger<MgTeam> _Logger;
        private readonly Config.Backup _Options;
        private readonly IGraphClientService _GraphClientService;
        private readonly string _TeamId;

        public MgTeam(ILogger<MgTeam> logger, Config.Backup options, IGraphClientService graphClientService, string teamid)
        {
            _Logger = logger;
            _Options = options;
            _GraphClientService = graphClientService;
            _TeamId = teamid;
        }

        public async Task Save()
        {
            await SaveTeam();
            await SaveMembers();
        }

        public async Task SaveTeam()
        {
            //Request all properties
            var request = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Request();

            Team detail = null;
            for (int i = 1; i <= MgGraphRequester.MaxRetry; i++)
            {
                try
                {
                    _Logger.LogTrace($"GraphURI({i}): {request.RequestUrl}");
                    detail = await request.GetAsync();
                    break;
                }
                catch (ServiceException mgsex)
                {
                    if (!await MgGraphRequester.ShouldContinue(mgsex, i))
                    {
                        throw;
                    }
                }
            }

            var jsonFile = GetBackupTeamFile(_Options.Path, _TeamId);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"File: {jsonFile}");
            await JsonSerializer.SerializeAsync<Team>(fs, detail, _Options.JsonOptions);
        }


        public async Task SaveMembers()
        {
            var memberPageRequest = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Members.Request();
            
            var listMember = new List<AadUserConversationMember>();
            do
            {

                ITeamMembersCollectionPage memberPage = null;
                for (int i = 1; i <= MgGraphRequester.MaxRetry; i++)
                {
                    try
                    {
                        _Logger.LogTrace($"MembersGraphURI({i}): {memberPageRequest.RequestUrl}");
                        memberPage = await memberPageRequest.GetAsync();
                        break;
                    }
                    catch (ServiceException mgsex)
                    {
                        if(!await MgGraphRequester.ShouldContinue(mgsex, i))
                        {
                            throw;
                        }
                    }
                }

                foreach (AadUserConversationMember member in memberPage)
                {
                    listMember.Add(member);
                    _Logger.LogTrace($"Member: {member.UserId}|{member.Email}|{member.DisplayName}");
                }
                memberPageRequest = memberPage.NextPageRequest;
            }
            while (memberPageRequest != null);

            var jsonFile = GetBackupTeamMembersFile(_Options.Path, _TeamId);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"MemberFile: {jsonFile}");
            await JsonSerializer.SerializeAsync<List<AadUserConversationMember>>(fs, listMember, _Options.JsonOptions);
        }


        public static string GetBackupPath(string root, string teamId)
        {
            var fullpath = System.IO.Path.Combine(root, teamId);
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetBackupTeamFile(string root, string teamId)
        {
            var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId), "team.json");
            return fullpath;
        }
        public static string GetBackupTeamMembersFile(string root, string teamId)
        {
            var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId), "team.members.json");
            return fullpath;
        }
    }
}
