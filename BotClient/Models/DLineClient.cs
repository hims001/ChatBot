using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;

namespace BotClient.Models
{
    public class DLineClient
    {
        private string directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
        private string botId = ConfigurationManager.AppSettings["BotId"];
        private string fromUser = "DirectLineSampleClientUser";
        DirectLineClient client;
        Conversation conversation;

        public Conversation StartBotConversation()
        {
            client = new DirectLineClient(directLineSecret);

            conversation = client.Conversations.StartConversation();
            return conversation;
        }

        private void ReceiveMessage(DirectLineClient client, string conversationId)
        {
            string watermark = null;

            var activitySet = client.Conversations.GetActivities(conversationId, watermark);
            watermark = activitySet?.Watermark;

            var activities = from x in activitySet.Activities
                             where x.From.Id == botId
                             select x;

            foreach (Activity activity in activities)
            {
                Console.WriteLine(activity.Text);

                if (activity.Attachments != null)
                {
                    foreach (Attachment attachment in activity.Attachments)
                    {
                        switch (attachment.ContentType)
                        {
                            case "application/vnd.microsoft.card.hero":
                                //RenderHeroCard(attachment);
                                break;

                            case "image/png":
                                //Console.WriteLine($"Opening the requested image '{attachment.ContentUrl}'");

                                //Process.Start(attachment.ContentUrl);
                                break;
                        }
                    }
                }
            }
        }

        public string SendMessage(string input)
        {
            ResourceResponse rr;
            if (input.Length > 0)
            {
                Activity userMessage = new Activity
                {
                    From = new ChannelAccount(fromUser),
                    Text = input,
                    Type = ActivityTypes.Message
                };

                rr = client.Conversations.PostActivity(conversation.ConversationId, userMessage);
                return rr.Id;
            }
            return string.Empty;
        }
    }


}