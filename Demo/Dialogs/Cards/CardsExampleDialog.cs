using AdaptiveCards;
using Demo.Accessors.State;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Dialogs.Cards
{
    public class CardsExampleDialog : ComponentDialog
    {
        //private const string GuestKey = nameof(GreetingDialog);
        private const string TextPrompt = "textPrompt";
        private const string ChoicePrompt = "choicePrompt";


        public CardsExampleDialog(string id) : base(id)
        {
            InitialDialogId = Id;

            // Define the prompts used in this conversation flow.
            AddDialog(new TextPrompt(TextPrompt));
            AddDialog(new ChoicePrompt(ChoicePrompt));

            // Define the conversation flow using a waterfall model.
            WaterfallStep[] waterfallSteps = new WaterfallStep[]
            {
                MenuStepAsync,
                HandleChoiceAsync,
                LoopBackAsync
            };
            AddDialog(new WaterfallDialog(Id, waterfallSteps));
        }

        //private static async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    await step.Context.SendActivityAsync("This dialog will show some examples of cards that can be used to present information.");
        //   // step.Values[GuestKey] = new GuestInfo();
        //    return await step.PromptAsync(
        //        TextPrompt,
        //        new PromptOptions
        //        {
        //            Prompt = MessageFactory.Text("What is your name?"),
        //        },
        //        cancellationToken);
        //}

        //private static async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    // Save the name
        //    string name = step.Result as string;

        //    await step.Context.SendActivityAsync(
        //        $"Thank you {name}, lets get started!",
        //        cancellationToken: cancellationToken);

        //    // End the dialog, returning the guest info.
        //    return await step.EndDialogAsync(
        //        null,
        //        cancellationToken);
        //}

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
                Text = "This is an example of a Hero Card.  Select an option to view other card options.",
                Subtitle = "This is the subtitle of the hero card.",
                Title = "This is the title of the hero card.",
                Images = new List<CardImage> { new CardImage("https://dev.botframework.com/Client/Images/ChatBot-BotFramework.png", "Example Image") },
                Buttons = new List<CardAction>()
                                    {
                                        new CardAction(ActionTypes.PostBack, title: "1. Adaptive Card", value: "1"),
                                        new CardAction(ActionTypes.PostBack, title: "2. Animation Card", value: "2"),
                                        new CardAction(ActionTypes.PostBack, title: "3. Audio Card", value: "3"),
                                        new CardAction(ActionTypes.PostBack, title: "4. Hero Card", value: "4"),
                                        new CardAction(ActionTypes.PostBack, title: "5. Thumbnail Card", value: "5"),
                                        new CardAction(ActionTypes.PostBack, title: "6. Receipt Card", value: "6"),
                                        new CardAction(ActionTypes.PostBack, title: "7. Signin Card", value: "7"),
                                        new CardAction(ActionTypes.PostBack, title: "8. Video Card", value: "8"),
                                        new CardAction(ActionTypes.PostBack, title: "9. Carousel Card", value: "9"),
                                        new CardAction(ActionTypes.PostBack, title: "10. Go Back", value: "10"),
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
            //UserInfo userInfo = await _accessors.UserInfoAccessor.GetAsync(stepContext.Context, null, cancellationToken);

            // Check the user's input and decide which dialog to start.
            // Pass in the guest info when starting either of the child dialogs.
            string choice = (stepContext.Result as string)?.Trim()?.ToLowerInvariant();
            switch (choice)
            {
                case "1":
                case "adaptive card":
                    //return await stepContext.BeginDialogAsync(CardsExampleDialogId, userInfo.Guest, cancellationToken);
                    var reply = stepContext.Context.Activity.CreateReply();

                    reply.Attachments = new List<Attachment>();

                    var adaptiveCardJson = await Demo.Cards.CardsUtility.GetCardText("Locations");
                    AdaptiveCardParseResult adaptiveCard;
                    try
                    {
                        adaptiveCard = AdaptiveCard.FromJson(adaptiveCardJson);
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }

                    var card = adaptiveCard.Card;

                    reply.Attachments.Add(new Attachment()
                    {
                        Content = card,
                        ContentType = AdaptiveCard.ContentType,
                        Name = "Card"
                    });

                    await stepContext.Context.SendActivityAsync("This is an example of an Adaptive Card.  Adaptive cards can be customized.  View more at https://adaptivecards.io");

                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "2":
                case "animation card":

                    var animationCardReply = stepContext.Context.Activity.CreateReply();

                    var animationCard = new AnimationCard()
                    {
                        Title = "Getting started with Microsoft Bot Framework and Azure Bot Services",
                        Text = "Animation Card Text",
                        Subtitle = "Animation Card Subtitle",
                        Buttons = new List<CardAction> { new CardAction(type: "messageBack", title: "Button 1") },
                        Media = new List<MediaUrl> { new MediaUrl(url: "https://www.youtube.com/watch?v=EP3ShiJVpW8") }

                    };

                    // Add the card to our reply.
                    animationCardReply.Attachments = new List<Attachment>() { animationCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(animationCardReply, cancellationToken);

                    // ContinueDialog doesn't wait for user input and keeps moving through the "waterfall"
                    return await stepContext.ContinueDialogAsync();
                case "3":
                case "audio card":

                    var audioCardReply = stepContext.Context.Activity.CreateReply();

                    var audioCard = new AudioCard()
                    {
                        Title = "Jazz Trio",
                        Text = "Audio Card Text",
                        Subtitle = "Audio Card Subtitle",
                        Media = new List<MediaUrl> { new MediaUrl(url: "https://ccrma.stanford.edu/~jos/mp3/JazzTrio.mp3") }

                    };

                    // Add the card to our reply.
                    audioCardReply.Attachments = new List<Attachment>() { audioCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(audioCardReply, cancellationToken);

                    string[] choices = new string[] { "This is great!", "Show me more." };
                    await stepContext.PromptAsync(
                        ChoicePrompt,
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text("What do you think?"),
                            Choices = ChoiceFactory.ToChoices(choices),
                        },
                        cancellationToken);

                    return Dialog.EndOfTurn;
                case "4":
                case "hero card":
                    // Present the user with a set of "suggested actions".
                    //List<string> menu = new List<string> { "Reserve Table", "Wake Up" };

                    //await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(menu, "How can I help you?"), cancellationToken: cancellationToken);

                    //return Dialog.EndOfTurn;

                    var heroCardReply = stepContext.Context.Activity.CreateReply();

                    // Create a HeroCard with options for the user to choose to interact with the bot.
                    var heroCard = new HeroCard
                    {
                        Text = "This is an example of a Hero Card.  Select an option to view other card options.",
                        Subtitle = "This is the subtitle of the hero card.",
                        Title = "This is the title of the hero card.",
                        Images = new List<CardImage> { new CardImage("https://dev.botframework.com/Client/Images/ChatBot-BotFramework.png", "Example Image"), new CardImage("https://dev.botframework.com/Client/Images/CognitiveServices.png", "Example Image 2") },
                        Buttons = new List<CardAction>()
                                    {
                                        new CardAction(ActionTypes.Call, title: "Call", value: "+11234567890"),
                                        new CardAction(ActionTypes.OpenUrl, title: "Open Url", value: "https://dev.botframework.com"),
                                    }
                    };

                    // Add the card to our reply.
                    heroCardReply.Attachments = new List<Attachment>() { heroCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(heroCardReply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "5":
                case "thumbnail card":

                    var thumbnailCardReply = stepContext.Context.Activity.CreateReply();

                    var thumbnailCard = new ThumbnailCard
                    {
                        Title = "Thumbnail Card Title",
                        Subtitle = "Subtitle",
                        Text = "Learn more about how to use Language Understanding with your bot.",
                        Buttons = new List<CardAction> { new CardAction("openUrl", "Learn More", value: "https://azure.microsoft.com/en-us/services/cognitive-services/language-understanding-intelligent-service/"), new CardAction("postBack", "Got it", value: "::gotit::") },
                        Images = new List<CardImage>
                        {
                             new CardImage("https://dev.botframework.com/Client/Images/learn-more-icons/luis.png", "LUIS", new CardAction("openUrl","Language Understanding", value: "https://azure.microsoft.com/en-us/services/cognitive-services/language-understanding-intelligent-service/")),
                        },

                    };

                    thumbnailCardReply.Attachments = new List<Attachment>() { thumbnailCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(thumbnailCardReply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "6":
                case "receipt card":

                    var receiptCardReply = stepContext.Context.Activity.CreateReply();

                    var receiptCard = new ReceiptCard
                    {
                        Title = "Example Receipt Card",
                        Facts = new List<Fact>
                        {
                            new Fact("Name:", "Adam"),
                            new Fact("Company:", "Microsoft"),
                            new Fact("Location:","Redmond, WA"),
                            new Fact("Website:", "https://www.microsoft.com"),
                            new Fact("----------",""),
                            new Fact("Order Number:","912304"),
                            new Fact("Payment Method:","Visa *1234"),
                            new Fact("----------",""),
                        },
                        Items = new List<ReceiptItem>
                        {
                            new ReceiptItem("Bing Spell Check", "sku: 123456", "Spell check for your bot", new CardImage("https://dev.botframework.com/Client/Images/learn-more-icons/bing_spell_check.png"), "$1.00", "1000" ),
                            new ReceiptItem("Text Analytics API", "sku: 65421", "Analytics, Sentiment and more", new CardImage("https://dev.botframework.com/Client/Images/learn-more-icons/text_analytics_api.png"), "$2.00", "2000" )
                        },
                        Tax = "$12.45",
                        Total = "$5,000"
                    };

                    receiptCardReply.Attachments = new List<Attachment>() { receiptCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(receiptCardReply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "7":
                case "signin card":

                    var signinCardReply = stepContext.Context.Activity.CreateReply();

                    var signinCard = new SigninCard
                    {
                        Text = "Sign In to your Account",
                        Buttons = new List<CardAction>
                         {
                             new CardAction ("signin", "Sign In", value: "https://login.microsoftonline.com/common/oauth/v2.0/authorize?client_id={CLIENT_ID}&scope={SCOPE}")
                         }
                    };

                    signinCardReply.Attachments = new List<Attachment> { signinCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(signinCardReply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "8":
                case "video card":

                    var videoCardReply = stepContext.Context.Activity.CreateReply();

                    var videoCard = new VideoCard
                    {
                        Title = "Getting started with Microsoft Bot Framework and Azure Bot Services",
                        Text = "Animation Card Text",
                        Subtitle = "Animation Card Subtitle",
                        Buttons = new List<CardAction> { new CardAction(type: "messageBack", title: "Video Card Button") },
                        Media = new List<MediaUrl> { new MediaUrl(url: "https://www.youtube.com/watch?v=EP3ShiJVpW8") },

                    };

                    videoCardReply.Attachments = new List<Attachment> { videoCard.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(videoCardReply, cancellationToken);

                    return Dialog.EndOfTurn;
                case "9":
                case "carousel card":

                    var carouselCardReply = stepContext.Context.Activity.CreateReply();

                    carouselCardReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    var carouselCard1 = new HeroCard
                    {
                        Title = "Title 1",
                        Text = "Text 1",
                        Images = new List<CardImage> { new CardImage("https://dev.botframework.com/Client/Images/learn-more-icons/bing_speech_api.png") }
                    };

                    var carouselCard2 = new HeroCard
                    {
                        Title = "Title 2",
                        Text = "Text 2",
                        Images = new List<CardImage> { new CardImage("https://dev.botframework.com/Client/Images/learn-more-icons/luis.png") }
                    };

                    carouselCardReply.Attachments = new List<Attachment> { carouselCard1.ToAttachment(), carouselCard2.ToAttachment() };

                    await stepContext.Context.SendActivityAsync(carouselCardReply, cancellationToken);

                    return Dialog.EndOfTurn;

                case "10":
                case "go back":
                    return await stepContext.EndDialogAsync();
                default:
                    // If we don't recognize the user's intent, start again from the beginning.
                    await stepContext.Context.SendActivityAsync(
                        "Sorry, I don't understand that command. Please choose an option from the list.");
                    return await stepContext.ReplaceDialogAsync(Id, null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> LoopBackAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (stepContext.Context.Activity.Value != null)
            {
                await stepContext.Context.SendActivityAsync($"You Entered: {stepContext.Context.Activity.Value}");
            }

            await stepContext.Context.SendActivityAsync("Try another example");
            //stepContext.
            // Restart the  menu dialog.
            return await stepContext.ReplaceDialogAsync(Id, null, cancellationToken);
        }
        #endregion
    }
}
