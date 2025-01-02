// Generated with CoreBot .NET Template version v4.22.0

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ILogger _logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(BookingDialog bookingDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, FlightBooking luisResult, CancellationToken cancellationToken)
        {
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (unsupportedCities.Any())
            {
                var messageText = $"Sorry but the following airports are not supported: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"Book a flight from Paris to Berlin on March 22, 2020\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // What choice did the user select
            switch ((string)stepContext.Result)
            {
                case "opening hours":
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), cancellationToken: cancellationToken);
                // Start a child dialog to see what the opening hours are
                default:
                    // Skip to next step in the waterfall
                    return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
