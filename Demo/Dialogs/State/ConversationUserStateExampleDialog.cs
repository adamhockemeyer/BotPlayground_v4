using Demo.Accessors.State;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Dialogs.State
{
    public class ConversationUserStateExampleDialog : ComponentDialog
    {
        public StateBotAccessors Accessors { get; }

        // Prompt Names
        private const string NamePrompt = "namePrompt";
        private const string RatingPrompt = "ratingPrompt";


        public ConversationUserStateExampleDialog(StateBotAccessors accessor, string id) : base(id)
        {
            InitialDialogId = Id;

            Accessors = accessor ?? throw new ArgumentNullException(nameof(accessor));
            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(NamePrompt));
            AddDialog(new ChoicePrompt(RatingPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                RatingStepAsync,
                FinalStepAsync
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            var userState = await Accessors.UserInfoAccessor.GetAsync(step.Context, () => null);

            if (!string.IsNullOrEmpty(userState.Guest.Name))
            {
                await step.Context.SendActivityAsync($"Great, we already have your name {userState.Guest.Name}!  Just one more question.");

                return await step.ContinueDialogAsync();
            }
            else
            {
                return await step.PromptAsync(
                               NamePrompt,
                               new PromptOptions
                               {
                                   Prompt = MessageFactory.Text("What is your name?"),
                               },
                               cancellationToken);
            }


        }

        private async Task<DialogTurnResult> RatingStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {

            var userState = await Accessors.UserInfoAccessor.GetAsync(step.Context, () => null);

            if (userState.Guest?.Name == null)
            {
                var result = step.Result as string;

                userState.Guest.Name = result;

                await Accessors.UserInfoAccessor.SetAsync(step.Context, userState);
            }



            List<Choice> choices = new List<Choice>()
            {
                new Choice("1"),
                new Choice("2"),
                new Choice("3"),
                new Choice("4"),
                new Choice("5")
            };

            string ratingMsg = "How would you rate this Bot?";

            if(userState.Guest.Rating != null)
            {
                ratingMsg += $" You gave it a {userState.Guest.Rating} last time FYI.";
            }

            return await step.PromptAsync(
                RatingPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text(ratingMsg),
                    Choices = choices
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Save the rating
            var rating = step.Result as FoundChoice;

            var userState = await Accessors.UserInfoAccessor.GetAsync(step.Context, () => null);

            userState.Guest.Rating = rating.Value;

            await Accessors.UserInfoAccessor.SetAsync(step.Context, userState);

            await step.Context.SendActivityAsync($"Thanks {userState.Guest.Name} for your feedback and rating of {userState.Guest.Rating}.", cancellationToken: cancellationToken);

            // End the dialog
            return await step.EndDialogAsync();
        }


    }
}
