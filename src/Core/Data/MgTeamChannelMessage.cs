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
    public class MgTeamChannelMessage
    {
        private readonly ILogger<MgTeamChannel> _Logger;
        private readonly Config.Backup _Options;
        private readonly IGraphClientService _GraphClientService;
        private readonly string _TeamId;
        private readonly string _ChannelId;
        private readonly string _MessageId;
        public MgTeamChannelMessage(ILogger<MgTeamChannel> logger, Config.Backup options, IGraphClientService graphClientService, string teamid, string channelid, string messageid)
        {
            _Logger = logger;
            _Options = options;
            _GraphClientService = graphClientService;
            _TeamId = teamid;
            _ChannelId = channelid;
            _MessageId = messageid;
        }

        public async Task Save()
        {
            await SaveMessage();
            await SaveMessageReplies();
        }

        public async Task Save(ChatMessage detail)
        {
            await SaveChatMessage(detail);
            await SaveMessageHostedContents(detail);

            if (detail.DeletedDateTime != null)
            {
                await SaveMessageReplies();
            }
        }

        public async Task SaveMessage()
        {
            //Request all properties
            var detailRequest = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Messages[_MessageId].Request();
            _Logger.LogTrace($"GraphURI: {detailRequest.RequestUrl}");
            try
            {
                var detail = await detailRequest.GetAsync();
                _Logger.LogTrace($"Message: {detail.Id}|{detail.Subject}|{detail.CreatedDateTime}");

                await Save(detail);
            }
            catch(Microsoft.Graph.ServiceException messageEx)
            {
                if (messageEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _Logger.LogWarning(messageEx, $"NotFound while processing root message: {detailRequest.RequestUrl}");
                }
                else
                {
                    _Logger.LogError(messageEx, $"Error while processing root message: {detailRequest.RequestUrl}");
                }
            }
        }
        public async Task SaveMessageReplies()
        {
            //Request all properties
            var detailRequest = _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Messages[_MessageId].Replies.Request();
            
            try
            {
                do
                {
                    _Logger.LogTrace($"GraphURI: {detailRequest.RequestUrl}");
                    var messageRepliesPage = await detailRequest.GetAsync();
                    foreach (var messageReply in messageRepliesPage)
                    {
                        _Logger.LogTrace($"MessageReply: {messageReply.Id}|{messageReply.ReplyToId}|{messageReply.CreatedDateTime}");

                        await SaveChatMessage(messageReply);
                        await SaveMessageHostedContents(messageReply);
                    }
                    detailRequest = messageRepliesPage.NextPageRequest;
                }
                while (detailRequest != null);
            }
            catch (Microsoft.Graph.ServiceException messageReplyEx)
            {
                if (messageReplyEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _Logger.LogWarning(messageReplyEx, $"NotFound while processing reply messages: {detailRequest.RequestUrl}");
                }
                else
                {
                    _Logger.LogError(messageReplyEx, $"Error while processing reply messages: {detailRequest.RequestUrl}");
                }
            }

        }
        
        public async Task SaveChatMessage(ChatMessage message)
        {
            var jsonFile = GetBackupMessageFile(_Options.Path, _TeamId, _ChannelId, message);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"File: {jsonFile}");
            await JsonSerializer.SerializeAsync<ChatMessage>(fs, message, _Options.JsonOptions);
        }

        public async Task SaveHostedContentsCollection(ChatMessage message, IChatMessageHostedContentsCollectionPage list)
        {
            var jsonFile = GetBackupMessageHostedContentFile(_Options.Path, _TeamId, _ChannelId, message);
            using System.IO.FileStream fs = System.IO.File.Create(jsonFile);
            _Logger.LogTrace($"File: {jsonFile}");
            await JsonSerializer.SerializeAsync<IChatMessageHostedContentsCollectionPage>(fs, list, _Options.JsonOptions);
        }

        public async Task SaveMessageHostedContents(ChatMessage message)
        {
            if (message.DeletedDateTime != null)
            {
                return;
            }
            
            var detailRequestBuilder =
                message.ReplyToId == null
                ? _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Messages[message.Id].HostedContents
                : _GraphClientService.GetGraphClient(_Logger).Teams[_TeamId].Channels[_ChannelId].Messages[message.ReplyToId].Replies[message.Id].HostedContents;

            var detailRequest = detailRequestBuilder.Request();
            IChatMessageHostedContentsCollectionPage list = new ChatMessageHostedContentsCollectionPage();

            do
            {
                _Logger.LogTrace($"GraphURI: {detailRequest.RequestUrl}");
                try
                {
                    var hostedContentPage = await detailRequest.GetAsync();
                    foreach (var hostedContent in hostedContentPage)
                    {
                        _Logger.LogTrace($"HostedContent: {message.Id}|{hostedContent.Id}");
                        list.Add(hostedContent);
                        await SaveHostedContentBlob(message, hostedContent, detailRequestBuilder);

                    }
                    detailRequest = hostedContentPage.NextPageRequest;
                }
                catch(Microsoft.Graph.ServiceException hostedContentEx)
                {
                    if (hostedContentEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _Logger.LogWarning(hostedContentEx, $"NotFound while processing hosted content: {detailRequest.RequestUrl}");
                    }
                    else
                    {
                        _Logger.LogError(hostedContentEx, $"Error while processing hosted content: {detailRequest.RequestUrl}");
                    }
                }
            }
            while (detailRequest != null);
            if (list.Count > 0)
            {
                await SaveHostedContentsCollection(message, list);
            }

        }

        public async Task SaveHostedContentBlob(ChatMessage message, ChatMessageHostedContent hostedContent, IChatMessageHostedContentsCollectionRequestBuilder detailRequestBuilder)
        {
            var messageHostedcontentRequest = detailRequestBuilder[hostedContent.Id].Content.Request();
            var messageHostedcontentValue = await messageHostedcontentRequest.GetAsync();

            var blobFile = GetBackupMessageHostedContentBlob(_Options.Path, _TeamId, _ChannelId, message, hostedContent);
            using System.IO.FileStream fs = System.IO.File.Create(blobFile);
            _Logger.LogTrace($"BlobFile: {blobFile}");
            await messageHostedcontentValue.CopyToAsync(fs);
        }

        public static string GetBackupPath(string root, string teamId, string channelId, string messageId)
        {
            var fullpath = System.IO.Path.Combine(MgTeamChannel.GetBackupPath(root, teamId, channelId), messageId);
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public const string MessageReplyFilePattern = "message.*.json";

        public static string GetBackupMessageFile(string root, string teamId, string channelId, string messageId, string? messageReplyId = null)
        {
            if (messageReplyId == null)
            {
                var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId, messageId), $"message.json");
                return fullpath;
            }
            else
            {
                var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId, messageReplyId), $"message.{messageId}.json");
                return fullpath;
            }
        }

        public static string GetBackupMessageFile(string root, string teamId, string channelId, ChatMessage message)
        {
            return GetBackupMessageFile(root, teamId, channelId, message.Id, message.ReplyToId);
        }
        public static string GetBackupMessageHostedContentFile(string root, string teamId, string channelId, ChatMessage message)
        {
            return GetBackupMessageHostedContentFile(root, teamId, channelId, message.Id, message.ReplyToId);
        }

        public static string GetBackupMessageHostedContentFile(string root, string teamId, string channelId, string messageId, string? messageReplyId = null)
        {
            if (messageReplyId == null)
            {
                var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId, messageId), $"hostedcontent.json");
                return fullpath;
            }
            else
            {
                var fullpath = System.IO.Path.Combine(GetBackupPath(root, teamId, channelId, messageReplyId), $"hostedcontent.{messageId}.json");
                return fullpath;
            }
        }
        public static string GetBackupMessageHostedContentBlob(string root, string teamId, string channelId, ChatMessage message, string hostedContentId)
        {
            return System.IO.Path.ChangeExtension(GetBackupMessageHostedContentFile(root, teamId, channelId, message), $".{Util.MD5Hash.Get(hostedContentId)}.png");

        }

        public static string GetBackupMessageHostedContentBlob(string root, string teamId, string channelId, ChatMessage message, ChatMessageHostedContent hostedContent)
        {
            return GetBackupMessageHostedContentBlob(root, teamId, channelId, message, hostedContent.Id);
        }
    }
}
