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

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_recognizer.IsConfigured)
            {
                throw new InvalidOperationException("ERROR: Model not ready");
            }

            var messageText = stepContext.Options?.ToString() ?? "What would you like to do?";
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput) }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = await _recognizer.RecognizeAsync<AutoGarageBotModel>(stepContext.Context, cancellationToken);

            switch (result.GetTopIntent().intent)
            {
                case AutoGarageBotModel.Intent.OpeningHours:
                    return await stepContext.BeginDialogAsync(nameof(OpeningHoursDialog), cancellationToken: cancellationToken);
                case AutoGarageBotModel.Intent.MakeAppointment:
                    var appointmentDetails = new AppointmentDetails
                    {
                        AppointmentDate = DateTime.Now.ToString(),
                        RepairType = new RepairType { RepairName = "Default Repair" },
                        TimeSlot = new TimeSlot { StartTime = "09:00" },
                        Customer = new CustomerDetails
                        {
                            LicensePlate = result.Entities.GetLicensePlate(),
                            FirstName = result.Entities.GetFirstName(),
                            LastName = result.Entities.GetLastName(),
                            Mail = result.Entities.GetMail(),
                            PhoneNumber = result.Entities.GetPhoneNumber()
                        }
                    };
                    return await stepContext.BeginDialogAsync(nameof(AppointmentDialog), appointmentDetails, cancellationToken);

                case AutoGarageBotModel.Intent.RepairTypes:
                    return await stepContext.BeginDialogAsync(nameof(RepairTypesDialog), cancellationToken: cancellationToken);
                default:
                    return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}