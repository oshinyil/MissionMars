using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MissionMars.Utils;

namespace MissionMars.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string _category;
        private string _severity;
        private string _description;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            await context.PostAsync("Hi! I’m the help desk bot and I can help you create a ticket.");
            PromptDialog.Text(context, DescriptionMessageReceivedAsync, "First, please briefly describe your problem to me.");
        }
        public async Task DescriptionMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            _description = await argument;
            var severities = new[] { "high", "normal", "low" };
            PromptDialog.Choice(context, SeverityMessageReceivedAsync, severities, "Which is the severity of this problem?");
        }

        public async Task SeverityMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            _severity = await argument;
            PromptDialog.Text(context, CategoryMessageReceivedAsync, "Which would be the category for this ticket (software, hardware, networking, security or other)?");
        }

        public async Task CategoryMessageReceivedAsync(IDialogContext context, IAwaitable<string> argument)
        {
            _category = await argument;
            var text = $"Great! I'm going to create a \"{_severity}\" severity ticket in the \"{_category}\" category. " +
                       $"The description I will use is \"{_description}\". Can you please confirm that this information is correct?";

            PromptDialog.Confirm(context, IssueConfirmedMessageReceivedAsync, text);
        }

        public async Task IssueConfirmedMessageReceivedAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirmed = await argument;

            if (confirmed)
            {
                var api = new TicketApiClient();
                var ticketId = await api.PostTicketAsync(_category, _severity, _description);

                if (ticketId != -1)
                {
                    var message = context.MakeMessage();
                    message.Attachments = new List<Attachment>
                    {
                        new Attachment
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = CreateCard(ticketId, _category, _severity, _description)
                        }
                    };

                    await context.PostAsync(message);
                }
                else
                {
                    await context.PostAsync("Ooops! Something went wrong while I was saving your ticket. Please try again later.");
                }
            }
            else
            {
                await context.PostAsync("Ok. The ticket was not created. You can start again if you want.");
            }

            context.Done<object>(null);
        }

        private static AdaptiveCard CreateCard(int ticketId, string category, string severity, string description)
        {
            var card = new AdaptiveCard();

            var headerBlock = new TextBlock()
            {
                Text = $"Ticket #{ticketId}",
                Weight = TextWeight.Bolder,
                Size = TextSize.Large,
                Speak = $"<s>You've created a new Ticket #{ticketId}</s><s>We will contact you soon.</s>"
            };

            var columnsBlock = new ColumnSet()
            {
                Separation = SeparationStyle.Strong,
                Columns = new List<Column>
                {
                    new Column
                    {
                        Size = "1",
                        Items = new List<CardElement>
                        {
                            new FactSet
                            {
                                Facts = new List<AdaptiveCards.Fact>
                                {
                                    new AdaptiveCards.Fact("Severity:", severity),
                                    new AdaptiveCards.Fact("Category:", category),
                                }
                            }
                        }
                    },
                    new Column
                    {
                        Size = "auto",
                        Items = new List<CardElement>
                        {
                            new Image
                            {
                                Url = "https://raw.githubusercontent.com/GeekTrainer/help-desk-bot-lab/master/assets/botimages/head-smiling-medium.png",
                                Size = ImageSize.Small,
                                HorizontalAlignment = HorizontalAlignment.Right
                            }
                        }
                    }
                }
            };

            var descriptionBlock = new TextBlock
            {
                Text = description,
                Wrap = true
            };

            card.Body.Add(headerBlock);
            card.Body.Add(columnsBlock);
            card.Body.Add(descriptionBlock);

            return card;
        }
    }
}