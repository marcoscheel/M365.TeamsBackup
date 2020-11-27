using HtmlAgilityPack;
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
    public class HtmlTeamChannelMessage
    {
        private readonly ILogger<HtmlTeamChannelMessage> _Logger;
        private readonly Config.Html _Options;
        private readonly string _TeamId;
        private readonly string _ChannelId;
        private readonly string _MessageId;

        public HtmlTeamChannelMessage(ILogger<HtmlTeamChannelMessage> logger, Config.Html options, string teamid, string channelid, string messageid)
        {
            _Logger = logger;
            _Options = options;
            _TeamId = teamid;
            _ChannelId = channelid;
            _MessageId = messageid;
        }

        private ChatMessage _Message;
        public ChatMessage Message
        {
            get
            {
                return _Message;
            }
        }

        private List<ChatMessage> _Replies;
        public List<ChatMessage> Replies
        {
            get
            {
                return _Replies;
            }
        }

        #region Load

        public async Task Load()
        {
            await LoadMessage();
            await LoadReplies();
        }

        public async Task LoadMessage()
        {
            var jsonFile = MgTeamChannelMessage.GetBackupMessageFile(_Options.SourcePath, _TeamId, _ChannelId, _MessageId);
            _Message = await LoadMessageFile(jsonFile);
        }

        public async Task<ChatMessage> LoadMessageFile(string jsonFile)
        {
            _Logger.LogTrace($"File: {jsonFile}");

            using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);
            var message = await JsonSerializer.DeserializeAsync<ChatMessage>(fs);

            await LoadHostedContent(message);
            
            return message;
        }

        public async Task LoadReplies()
        {
            _Replies = new List<ChatMessage>();

            var jsonDirectory = MgTeamChannelMessage.GetBackupPath(_Options.SourcePath, _TeamId, _ChannelId, _MessageId);
            _Logger.LogTrace($"File: {jsonDirectory}");

            foreach(var jsonFile in System.IO.Directory.GetFiles(jsonDirectory, MgTeamChannelMessage.MessageReplyFilePattern))
            {
                _Replies.Add(await LoadMessageFile(jsonFile));
            }
        }

        private async Task LoadHostedContent(ChatMessage message)
        {
            var jsonFile = MgTeamChannelMessage.GetBackupMessageHostedContentFile(_Options.SourcePath, _TeamId, _ChannelId, message);
            _Logger.LogTrace($"File: {jsonFile}");

            if (System.IO.File.Exists(jsonFile))
            {
                using System.IO.FileStream fs = System.IO.File.OpenRead(jsonFile);
                message.HostedContents = await JsonSerializer.DeserializeAsync<ChatMessageHostedContentsCollectionPage>(fs);
            }
        }
        #endregion

        #region Save

        public async Task<HtmlNode> GetHtml(HtmlDocument htmlDocument)
        {
            if (_Message == null)
            {
                await LoadMessage();
            }
            if (_Replies == null)
            {
                await LoadReplies();
            }
            var threadNode = htmlDocument.CreateElement("thread");
            GetHtmlForPost(threadNode, _Message);

            foreach (var reply in _Replies.OrderBy(r => r.CreatedDateTime ))
            {
                GetHtmlForPost(threadNode, reply);
            }

            return threadNode;
        }

        private void GetHtmlForPost(HtmlNode threadNode, ChatMessage message)
        {
            var htmlDocument = threadNode.OwnerDocument;
            var messagetNode = message.ReplyToId == null ? htmlDocument.CreateElement("post") : htmlDocument.CreateElement("reply");
            threadNode.AppendChild(messagetNode);

            if (!string.IsNullOrEmpty(message.Subject))
            {
                var messageSubjectNode = htmlDocument.CreateElement("h2");
                messagetNode.AppendChild(messageSubjectNode);
                messageSubjectNode.InnerHtml = message.Subject;
            }

            var messageInfoNode = htmlDocument.CreateElement("chatmeta");
            messagetNode.AppendChild(messageInfoNode);
            messageInfoNode.InnerHtml = "Created: " + message.CreatedDateTime.Value.ToString(_Options.DateTimeFormat);
            if (message.LastEditedDateTime != null)
            {
                messageInfoNode.InnerHtml += " | Edited: " + message.LastEditedDateTime.Value.ToString(_Options.DateTimeFormat);
            }
            if (message.From?.User != null)
            {
                messageInfoNode.InnerHtml += " | Author: " + message.From.User.DisplayName;
            }
            if (message.From?.Application != null)
            {
                messageInfoNode.InnerHtml += " | Application: " + message.From.Application.DisplayName;
            }
            if (message.DeletedDateTime != null)
            {
                var messageBodyNode = htmlDocument.CreateElement("p");
                messagetNode.AppendChild(messageBodyNode);
                messageBodyNode.InnerHtml = "DELETED CONTENT";
            }
            else
            {
                var messageBodyNode = htmlDocument.CreateElement("p");
                messagetNode.AppendChild(messageBodyNode);

                messageBodyNode.InnerHtml = message.Body.Content;

                var imageNodes = messageBodyNode.SelectNodes(".//img");
                if (imageNodes != null)
                {
                    foreach (var imgNode in imageNodes)
                    {
                        if (imgNode.Attributes["src"].Value.EndsWith("$value"))
                        {
                            var chunks = imgNode.Attributes["src"].Value.Split('/');
                            var hostedContentid = chunks[chunks.Length - 2];

                            var blobFileName = MgTeamChannelMessage.GetBackupMessageHostedContentBlob(_Options.SourcePath, _TeamId, _ChannelId, message, hostedContentid);

                            if (_Options.UseInlineImages)
                            {
                                var blobFileb64 = System.Convert.ToBase64String(System.IO.File.ReadAllBytes(blobFileName));
                                imgNode.SetAttributeValue("src", $"data:image/png;base64,{blobFileb64}");

                            }
                            else
                            {
                                string outFilename = null;
                                if (message.ReplyToId == null)
                                {
                                    outFilename = GetOutputMessageImgFile(_Options.TargetPath, _TeamId, _ChannelId, message.Id, null, System.IO.Path.GetFileName(blobFileName));
                                }
                                else
                                {
                                    outFilename = GetOutputMessageImgFile(_Options.TargetPath, _TeamId, _ChannelId, message.ReplyToId, message.Id , System.IO.Path.GetFileName(blobFileName));

                                }
                                System.IO.File.Copy(blobFileName, outFilename, true);

                                imgNode.SetAttributeValue("src", $"./img/{System.IO.Path.GetFileName(outFilename)}");
                            }

                            imgNode.SetAttributeValue("class", "hc");
                        }

                        imgNode.SetAttributeValue("style", "");
                    }
                }

            }
        }
        #endregion

        public static string GetOutputPath(string root, string teamId, string channelId, string messageId)
        {
            var fullpath = HtmlTeamChannel.GetOutputPath(root, teamId, channelId);
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetOutputImgPath(string root, string teamId, string channelId, string messageId)
        {
            var fullpath = HtmlTeamChannel.GetOutputPath(root, teamId, $"{channelId}\\img");
            System.IO.Directory.CreateDirectory(fullpath);
            return fullpath;
        }
        public static string GetOutputMessageImgFile(string root, string teamId, string channelId, string messageId, string messageReplyId, string filename)
        {
            var fullpath = System.IO.Path.Combine(GetOutputImgPath(root, teamId, channelId, messageId), $"{filename}");
            return fullpath;
        }
        public static string GetOutputMessageFile(string root, string teamId, string channelId, string messageId, string messageReplyId = null)
        {
            var fullpath = System.IO.Path.Combine(GetOutputPath(root, teamId, channelId, messageId), $"{messageId}.html");
            return fullpath;
        }
    }
}
