// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using TaxiCompanyBot;

namespace Microsoft.BotBuilderSamples
{

    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoWithCounterBot : IBot
    {
        // Generic message to be sent to user
        //  private const string WelcomeMessage = "Hello User.. I'm Your asistant bot.... How can I help you...? ";

        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;


        public static string defaultmsg = "Sorry, I can not undestand what you said.\n";

        public static readonly string LuisKey = "MiniprojectLuisBot";

        private readonly BotServices _services;

        /// Services configured from the ".bot" file.


        /// <summary>
        /// Initializes a new instance of the <see cref="EchoWithCounterBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory, BotServices botServices)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }


            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            _services = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a LUIS service named '{LuisKey}'.");
            }
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var Convo = await _accessors.TopicState.GetAsync(turnContext, () => new TopicState());

            // Use state accessor to extract the didBotWelcomeUser flag
            var Userinputs = await _accessors.UserStateInputs.GetAsync(turnContext, () => new UserStateInputs());

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {

                if (Userinputs.DidBotWelcomeUser == false)
                {
                    Userinputs.DidBotWelcomeUser = true;

                    await _accessors.UserStateInputs.SetAsync(turnContext, Userinputs);
                    await _accessors.UserState.SaveChangesAsync(turnContext);

                }

                if (Convo.Prompt == "AskName")
                {
                    //  await turnContext.SendActivityAsync("good morning Friend, What is your Name?", cancellationToken: cancellationToken);

                    var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                    var topIntent = recognizerResult?.GetTopScoringIntent();
                    if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                    {
                        await turnContext.SendActivityAsync($"==>LUIS Top Scoring Intent: {topIntent.Value.intent}, Score: {topIntent.Value.score}\n");
                    }
                    else
                    {
                        var msg = @"No LUIS intents were found.
                        This sample is about identifying two user intents:
                        'Calendar.Add'
                        'Calendar.Find'
                        Try typing 'Add Event' or 'Show me tomorrow'.";
                        await turnContext.SendActivityAsync(msg);
                    }

                    Convo.Prompt = "AskChoice";
                    await _accessors.TopicState.SetAsync(turnContext, Convo);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                }
                else if (Convo.Prompt == "AskChoice")
                {
                    Userinputs.UserName = turnContext.Activity.Text;
                    await turnContext.SendActivityAsync($"Hello, {Userinputs.UserName}", cancellationToken: cancellationToken);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);

                    Convo.Prompt = "ProvideInformation";
                    await _accessors.TopicState.SetAsync(turnContext, Convo);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);

                    await _accessors.UserStateInputs.SetAsync(turnContext, Userinputs);
                    await _accessors.UserState.SaveChangesAsync(turnContext);
                }
                else if (Convo.Prompt == "ProvideInformation")
                {
                    Userinputs.Choice = turnContext.Activity.Text;
                    var responsemsg = ProcessInput(Userinputs.Choice);
                    await turnContext.SendActivityAsync(responsemsg, cancellationToken: cancellationToken);

                    Convo.Prompt = "AskName";
                    await _accessors.TopicState.SetAsync(turnContext, Convo);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);
                }

            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded.Any())
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message
                        // the 'bot' is the recipient for events from the channel,
                        // turnContext.Activity.MembersAdded == turnContext.Activity.Recipient.Id indicates the
                        // bot was added to the conversation.
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync("TaxiOffice Pvt Limited \n  " +
                                "We are leading Sri lankan Taxi provider company that provides our " +
                                "services mainly to Office staff members.\nJohn,CEO \n" +
                                "hello...");
                        }
                    }
                }
                else
                {
                    // Default behaivor for all other type of events.
                    var ev = turnContext.Activity.AsEventActivity();
                    await turnContext.SendActivityAsync($"Received event: {ev.Name}");
                }
            }

            // save any state changes made to your state objects.
            await _accessors.UserState.SaveChangesAsync(turnContext);
        }

        private static string ProcessInput(string text)
        {

            switch (text)
            {
                case "Ask me Something":

                    return "Ok, What do you want to know? ";
                    break;
                case "Contact us":

                    return "Go to contacts..";
                    break;

                case "About us":

                    return "Go to about us.. ";
                    break;

                default:

                    return defaultmsg;
                    break;
            }
        }
        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = turnContext.Activity.CreateReply("How can I help You?");
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Ask me Something", Type = ActionTypes.ImBack, Value = "Ask me Something" },
                    new CardAction() { Title = "Contact us", Type = ActionTypes.ImBack, Value = "Contact us" },
                    new CardAction() { Title = "About us", Type = ActionTypes.ImBack, Value = "About us" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
