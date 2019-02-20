using Demo.Accessors.State;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Dialogs.Common
{
    public class GreetingDialog : ComponentDialog
    {
        private const string GuestKey = nameof(GreetingDialog);
        private const string TextPrompt = "textPrompt";

        // You can start this from the parent using the dialog's ID.
        public GreetingDialog(string id) : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                FinalStepAsync,
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext step,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            step.Values[GuestKey] = new GuestInfo();
            return await step.PromptAsync(
                TextPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What is your name?"),
                },
                cancellationToken);
        }

        private static async Task<DialogTurnResult> FinalStepAsync(
            WaterfallStepContext step,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Save the name
            string name = step.Result as string;

            ((GuestInfo)step.Values[GuestKey]).Name = name;

            await step.Context.SendActivityAsync(
                $"Thank you {name}, lets get started!",
                cancellationToken: cancellationToken);

            // End the dialog, returning the guest info.
            // This is an example of passing data back to an accessor to save.
            return await step.EndDialogAsync(
                (GuestInfo)step.Values[GuestKey],
                cancellationToken);
        }
    }
}
