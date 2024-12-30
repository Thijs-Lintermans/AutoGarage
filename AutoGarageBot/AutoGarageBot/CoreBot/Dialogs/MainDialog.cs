// Generated with CoreBot .NET Template version v4.22.0

using CoreBot.CognitiveModels;
using CoreBot.DialogDetails;
using CoreBot.Models;
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
            private readonly AutoGarageBotCLURecognizer _recognizer;

            // Dependency injection uses this constructor to instantiate MainDialog
            public MainDialog(AppointmentDialog appointmentDialog, OpeningHoursDialog openingHoursDialog, RepairTypesDialog repairTypesDialog, AutoGarageBotCLURecognizer autoGarageBotCLURecognizer, ILogger<MainDialog> logger)
                : base(nameof(MainDialog))
            {
                _logger = logger;
                _recognizer = autoGarageBotCLURecognizer;

                AddDialog(new TextPrompt(nameof(TextPrompt)));
                AddDialog(openingHoursDialog);
                AddDialog(appointmentDialog);
                AddDialog(repairTypesDialog);

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

            private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                if (!_recognizer.IsConfigured)
                {
                    throw new InvalidOperationException("ERROR: Model not ready");
                }
                // Show What would you like to do first time, What else second time
                var messageText = stepContext.Options?.ToString() ?? "What would you like to do?";
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput) }, cancellationToken);
            }

            private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                var result = await _recognizer.RecognizeAsync<AutoGarageBotModel>(stepContext.Context, cancellationToken);
            // What choice did the user select
                switch (result.GetTopIntent().intent)
                {
                    case AutoGarageBotModel.Intent.OpeningHours:
                        return await stepContext.BeginDialogAsync(nameof(OpeningHoursDialog), cancellationToken: cancellationToken);
                    // Start a child dialog to see what the opening hours are
                    case AutoGarageBotModel.Intent.MakeAppointment:
                        // Start a child dialog to see what the opening hours are
                        var customerDetails = new CustomerDetails();
                        customerDetails.LicensePlate = result.Entities.GetLicensePlate();
                        customerDetails.FirstName = result.Entities.GetFirstName();
                        customerDetails.LastName = result.Entities.GetLastName();
                        customerDetails.Mail = result.Entities.GetMail();
                        customerDetails.PhoneNumber = result.Entities.GetPhoneNumber();
                        var appointmentDetails = new AppointmentDetails();
                        appointmentDetails.AppointmentDate = result.Entities.GetAppointmentDate();
                        appointmentDetails.RepairType.RepairName = result.Entities.GetRepairType();
                        appointmentDetails.TimeSlot.StartTime = result.Entities.GetTimeSlot();

                        return await stepContext.BeginDialogAsync(nameof(AppointmentDialog), new Customer(), cancellationToken: cancellationToken);
                    case AutoGarageBotModel.Intent.RepairType:
                        // Start a child dialog to see what the opening hours are
                        return await stepContext.BeginDialogAsync(nameof(RepairTypesDialog), cancellationToken: cancellationToken);
                    default:
                        // Skip to next step in the waterfall
                        return await stepContext.NextAsync(null, cancellationToken);
                }
            }

            private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
            {
                // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
                // the Result here will be null.
                if (stepContext.Result is BookingDetails result)
                {
                    // Now we have all the booking details call the booking service.

                    // If the call to the booking service was successful tell the user.

                    var timeProperty = new TimexProperty(result.TravelDate);
                    var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                    var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                    var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(message, cancellationToken);
                }

                // Restart the main dialog with a different message the second time around
                var promptMessage = "What else can I do for you?";
                return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
            }
        }
    }
