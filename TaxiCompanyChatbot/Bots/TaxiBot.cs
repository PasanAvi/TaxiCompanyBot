// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.10.3

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace TaxiCompanyChatbot.Bots
{
    public class TaxiBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Text.ToLower().Trim())
            {
                case "register":
                    await turnContext.SendActivityAsync("You're here to register.");
                    break;

                case "contact us":
                    await turnContext.SendActivityAsync("You're here to get contacted.");
                    break;

                case "about us":
                    await turnContext.SendActivityAsync("You're here to know better us.");
                    break;

                case "ask me":
                    await turnContext.SendActivityAsync("You're here for the QnAs.");
                    break;

                default:
                    await turnContext.SendActivityAsync("I can't understand you.");
                    break;
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello friend..I'm your Taxi Assistant Bot.";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                    await SendWelcomeBtnsAsync(turnContext, cancellationToken);
                }
            }
        }

        //Welcome Msg buttons..!
        private static async Task SendWelcomeBtnsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("How can I help you?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
        {
            new CardAction() { Title = "Register", Type = ActionTypes.ImBack, Value = "Register" },
            new CardAction() { Title = "Contact Us", Type = ActionTypes.ImBack, Value = "Contact Us" },
            new CardAction() { Title = "About Us", Type = ActionTypes.ImBack, Value = "About Us" },
            new CardAction() { Title = "Ask me", Type = ActionTypes.ImBack, Value = "Ask me" },
        },

            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
