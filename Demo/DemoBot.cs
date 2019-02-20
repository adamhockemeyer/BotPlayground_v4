// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Demo.Accessors.State;
using Demo.Dialogs.Cards;
using Demo.Dialogs.Common;
using Demo.Dialogs.Hotel;
using Demo.Dialogs.State;
using Demo.Dialogs.Waterfall;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Demo
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class DemoBot : IBot
    {
        private readonly StateBotAccessors _accessors;
        private readonly ILogger _logger;
        private readonly DialogSet _dialogs;

        private const string WelcomeMessage = @"Welcome to the Demo bot.  This bot will introduce you to several concepts and features available in the Bot Framework.";

        // Define the dialog and prompt names for the bot.
        // Define the IDs for the dialogs in the bot's dialog set.
        private const string MainDialogId = "mainDialog";
        private const string GreetingDialogId = "greetingDialog";
        private const string CardsExampleDialogId = "cardsExampleDialog";
        private const string ConversationUserStateExampleDialogId = "conversationUserStateExampleDialog";
        private const string WaterfallDialogExampleId = "waterfallDialogExample";



        /// <summary>
        /// Initializes the bot and add dialogs and prompts to the dialog set.
        /// </summary>                        
        public DemoBot(StateBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<DemoBot>();
            _logger.LogTrace("DemoBot turn start.");

            // Define the steps of the main dialog.
            WaterfallStep[] steps = new WaterfallStep[]
            {
                MenuStepAsync,
                HandleChoiceAsync,
                LoopBackAsync,
            };

            // Create our bot's dialog set, adding a main dialog and the three component dialogs.
            _dialogs = new DialogSet(_accessors.DialogStateAccessor)
                .Add(new WaterfallDialog(MainDialogId, steps))
                .Add(new GreetingDialog(GreetingDialogId))
                .Add(new CardsExampleDialog(CardsExampleDialogId))
                .Add(new ConversationUserStateExampleDialog(_accessors,ConversationUserStateExampleDialogId))
                .Add(new WaterfallExampleDialog(WaterfallDialogExampleId));
                
        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the state properties from the turn context.

            ConversationData conversationData = await _accessors.ConversationDataAccessor.GetAsync(turnContext, () => new ConversationData());
            // Get the user's info.
            UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(turnContext, () => new UserInfo(), cancellationToken);

            _logger.LogInformation(turnContext.Activity.Type);

            // Establish dialog state from the conversation state.
            DialogContext dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Continue any current dialog.
                DialogTurnResult dialogTurnResult = await dc.ContinueDialogAsync();

                // Process the result of any complete dialog.
                if (dialogTurnResult.Status is DialogTurnStatus.Complete)
                {
                    switch (dialogTurnResult.Result)
                    {
                        case GuestInfo guestInfo:
                            // Store the results of the greeting dialog.
                            userInfo.Guest = guestInfo;
                            await _accessors.UserInfoAccessor.SetAsync(turnContext, userInfo, cancellationToken);

                            // Show the main menu dialog
                            await dc.BeginDialogAsync(MainDialogId, null, cancellationToken);
                            break;
                        default:
                            // We shouldn't get here, since the main dialog is designed to loop.
                            break;
                    }
                }

                // Every dialog step sends a response, so if no response was sent,
                // then no dialog is currently active.
                else if (!turnContext.Responded)
                {
                    // Otherwise, start our bot's main dialog.
                    await dc.BeginDialogAsync(MainDialogId, null, cancellationToken);
                }
            }
            // Greet when users are added to the conversation.
            // Note that all channels do not send the conversation update activity.
            // If you find that this bot works in the emulator, but does not in
            // another channel the reason is most likely that the channel does not
            // send this activity.
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                _logger.LogInformation("Welcome Message Area");

                if (turnContext.Activity.MembersAdded != null)
                {
                    // Iterate over all new members added to the conversation
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        // Greet anyone that was not the target (recipient) of this message
                        // the 'bot' is the recipient for events from the channel,
                        // turnContext.Activity.MembersAdded == turnContext.Activity.Recipient.Id indicates the
                        // bot was added to the conversation.
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync($"Hi there - {member.Name}. {WelcomeMessage}", cancellationToken: cancellationToken);
                            //await turnContext.SendActivityAsync($"What's your name?", cancellationToken: cancellationToken);

                            await dc.BeginDialogAsync(GreetingDialogId, null, cancellationToken);
                            // Can't start a dialog from ConversationUpdated
                            //await dc.BeginDialogAsync(MainDialogId, null, cancellationToken);
                        }
                    }
                }
            }

            // Save the new turn count into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        #region Waterfall Steps
        private static async Task<DialogTurnResult> MenuStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Present the user with a set of "suggested actions".
            //List<string> menu = new List<string> { "Reserve Table", "Wake Up" };

            //await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(menu, "How can I help you?"), cancellationToken: cancellationToken);

            //return Dialog.EndOfTurn;


            var reply = stepContext.Context.Activity.CreateReply();

            // Create a HeroCard with options for the user to choose to interact with the bot.
            var card = new HeroCard
            {
                Text = "Welcome to the Demo bot!",
                Subtitle = "Select an option below to get started:",
                Buttons = new List<CardAction>()
                                    {
                                        new CardAction(ActionTypes.PostBack, title: "1. Cards Example", value: "1"),
                                        new CardAction(ActionTypes.PostBack, title: "2. Conversation & User State Example", value: "2"),
                                        new CardAction(ActionTypes.PostBack, title: "3. Waterfall Dialog Example", value: "3"),
                                        new CardAction(ActionTypes.PostBack, title: "4. (LUIS) Language Understanding Example", value: "4"),
                                        new CardAction(ActionTypes.PostBack, title: "4. Cognitive Services Example", value: "5"),
                                    },
            };

            // Add the card to our reply.
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            return Dialog.EndOfTurn;
        }

        private async Task<DialogTurnResult> HandleChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the user's info. (Since the type factory is null, this will throw if state does not yet have a value for user info.)
            UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, null, cancellationToken);

            // Check the user's input and decide which dialog to start.
            // Pass in the guest info when starting either of the child dialogs.
            string choice = (stepContext.Result as string)?.Trim()?.ToLowerInvariant();
            switch (choice)
            {
                case "1":
                case "cards example":
                    return await stepContext.BeginDialogAsync(CardsExampleDialogId, userInfo.Guest, cancellationToken);

                case "2":
                case "conversation & user state example":
                    return await stepContext.BeginDialogAsync(ConversationUserStateExampleDialogId, null, cancellationToken);
                case "3":
                case "waterfall dialog example":
                    return await stepContext.BeginDialogAsync(WaterfallDialogExampleId, null, cancellationToken);
                default:
                    // If we don't recognize the user's intent, start again from the beginning.
                    await stepContext.Context.SendActivityAsync(
                        "Sorry, I don't understand that command. Please choose an option from the list.");
                    return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> LoopBackAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the user's info. (Because the type factory is null, this will throw if state does not yet have a value for user info.)
            UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, null, cancellationToken);

            // Process the return value from the child dialog.
            switch (stepContext.Result)
            {
                case TableInfo table:
                    // Store the results of the reserve-table dialog.
                    userInfo.Table = table;
                    await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
                    break;
                case WakeUpInfo alarm:
                    // Store the results of the set-wake-up-call dialog.
                    userInfo.WakeUp = alarm;
                    await _accessors.UserInfoAccessor.SetAsync(stepContext.Context, userInfo, cancellationToken);
                    break;
                default:
                    // We shouldn't get here, since these are no other branches that get this far.
                    break;
            }

            // Restart the main menu dialog.
            return await stepContext.ReplaceDialogAsync(MainDialogId, null, cancellationToken);
        }
        #endregion
    }
}
