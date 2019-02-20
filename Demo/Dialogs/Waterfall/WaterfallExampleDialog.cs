using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Dialogs.Waterfall
{
    // From: https://github.com/Microsoft/BotFramework-Samples/blob/master/SDKV4-Samples/dotnet_core/DialogPromptBot/DialogPromptBot.cs


    public class WaterfallExampleDialog : ComponentDialog
    {
        // Define identifiers for our dialogs and prompts.
        private const string ReservationDialog = "reservationDialog";
        private const string PartySizePrompt = "partyPrompt";
        private const string LocationPrompt = "locationPrompt";
        private const string ReservationDatePrompt = "reservationDatePrompt";
        public WaterfallExampleDialog(string id) : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new NumberPrompt<int>(PartySizePrompt, PartySizeValidatorAsync));
            AddDialog(new ChoicePrompt(LocationPrompt));
            AddDialog(new DateTimePrompt(ReservationDatePrompt, DateValidatorAsync));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                PromptForPartySizeAsync,
                PromptForLocationAsync,
                PromptForReservationDateAsync,
                AcknowledgeReservationAsync,
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        /// <summary>First step of the main dialog: prompt for party size.</summary>
        /// <param name="stepContext">The context for the waterfall step.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result contains information from this step.</remarks>
        private async Task<DialogTurnResult> PromptForPartySizeAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await stepContext.Context.SendActivityAsync("Welcome to the Waterfall Dialog Example!");

            await stepContext.Context.SendActivityAsync("Let's go through an example of making a dinner reservation...");


            // Prompt for the party size. The result of the prompt is returned to the next step of the waterfall.
            return await stepContext.PromptAsync(
                PartySizePrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("How many people is the reservation for?"),
                    RetryPrompt = MessageFactory.Text("How large is your party?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForLocationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Record the party size information in the current dialog state.
            int size = (int)stepContext.Result;
            stepContext.Values["size"] = size;

            return await stepContext.PromptAsync(
                LocationPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please choose a location."),
                    RetryPrompt = MessageFactory.Text("Sorry, please choose a location from the list."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Redmond", "Bellevue", "Seattle" }),
                },
                cancellationToken);
        }

        /// <summary>Second step of the main dialog: record the party size and prompt for the
        /// reservation date.</summary>
        /// <param name="stepContext">The context for the waterfall step.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result contains information from this step.</remarks>
        private async Task<DialogTurnResult> PromptForReservationDateAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Record the party size information in the current dialog state.
            var location = stepContext.Result;
            stepContext.Values["location"] = location;

            // Prompt for the party size. The result of the prompt is returned to the next step of the waterfall.
            return await stepContext.PromptAsync(
                ReservationDatePrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Great. When will the reservation be for?"),
                    RetryPrompt = MessageFactory.Text("What time should we make your reservation for?"),
                },
                cancellationToken);
        }

        /// <summary>Third step of the main dialog: return the collected party size and reservation date.</summary>
        /// <param name="stepContext">The context for the waterfall step.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the task is successful, the result contains information from this step.</remarks>
        private async Task<DialogTurnResult> AcknowledgeReservationAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Retrieve the reservation date.
            DateTimeResolution resolution = (stepContext.Result as IList<DateTimeResolution>).First();
            string time = resolution.Value ?? resolution.Start;

            // Send an acknowledgement to the user.
            await stepContext.Context.SendActivityAsync(
                "Thank you. We will confirm your reservation shortly.",
                cancellationToken: cancellationToken);

            // Return the collected information to the parent context.
            Reservation reservation = new Reservation
            {
                Date = time,
                Size = (int)stepContext.Values["size"],
            };
            return await stepContext.EndDialogAsync(reservation, cancellationToken);
        }

        /// <summary>Validates whether the party size is appropriate to make a reservation.</summary>
        /// <param name="promptContext">The validation context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>Reservations can be made for groups of 6 to 20 people.
        /// If the task is successful, the result indicates whether the input was valid.</remarks>
        private async Task<bool> PartySizeValidatorAsync(
            PromptValidatorContext<int> promptContext,
            CancellationToken cancellationToken)
        {
            // Check whether the input could be recognized as an integer.
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                    "I'm sorry, I do not understand. Please enter the number of people in your party.",
                    cancellationToken: cancellationToken);
                return false;
            }

            // Check whether the party size is appropriate.
            int size = promptContext.Recognized.Value;
            if (size < 6 || size > 20)
            {
                await promptContext.Context.SendActivityAsync(
                    "Sorry, we can only take reservations for parties of 6 to 20.",
                    cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }

        /// <summary>Validates whether the reservation date is appropriate.</summary>
        /// <param name="promptContext">The validation context.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>Reservations must be made at least an hour in advance.
        /// If the task is successful, the result indicates whether the input was valid.</remarks>
        private async Task<bool> DateValidatorAsync(
            PromptValidatorContext<IList<DateTimeResolution>> promptContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Check whether the input could be recognized as an integer.
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync(
                    "I'm sorry, I do not understand. Please enter the date or time for your reservation.",
                    cancellationToken: cancellationToken);
                return false;
            }

            // Check whether any of the recognized date-times are appropriate,
            // and if so, return the first appropriate date-time.
            DateTime earliest = DateTime.Now.AddHours(1.0);
            DateTimeResolution value = promptContext.Recognized.Value.FirstOrDefault(v =>
                DateTime.TryParse(v.Value ?? v.Start, out DateTime time) && DateTime.Compare(earliest, time) <= 0);
            if (value != null)
            {
                promptContext.Recognized.Value.Clear();
                promptContext.Recognized.Value.Add(value);
                return true;
            }

            await promptContext.Context.SendActivityAsync(
                    "I'm sorry, we can't take reservations earlier than an hour from now.",
                    cancellationToken: cancellationToken);
            return false;
        }

        public class Reservation
        {
            public int Size { get; set; }

            public string Date { get; set; }
        }
    }
}
