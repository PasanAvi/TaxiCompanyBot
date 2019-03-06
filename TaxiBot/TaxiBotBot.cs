// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace TaxiBot
{
   
    public class TaxiBotBot : IBot
    {
        private readonly TaxiBotAccessors _accessors;
        private readonly ILogger _logger;

      
        public TaxiBotBot(ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _accessors = new TaxiBotAccessors(conversationState)
            {
                CounterState = conversationState.CreateProperty<CounterState>(TaxiBotAccessors.CounterStateName),
            };

            _logger = loggerFactory.CreateLogger<TaxiBotBot>();
            _logger.LogTrace("Turn start.");
        }

       
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {

            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                foreach (var member in turnContext.Activity.MembersAdded)
                {
                    if (member.Id != turnContext.Activity.Recipient.Id)
                    {
                        var greet = turnContext.Activity.CreateReply("Hello friend..I'm your Taxi Assistant Bot.");
                        await turnContext.SendActivityAsync(greet, cancellationToken);
                        await SendWelcomeBtnsAsync(turnContext, cancellationToken);

                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

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
