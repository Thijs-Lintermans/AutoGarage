﻿using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using CoreBot.Helpers;
using CoreBot.Cards;

namespace CoreBot.Dialogs
{
    public class RepairTypesDialog : CancelAndHelpDialog
    {
        public RepairTypesDialog()
            : base(nameof(RepairTypesDialog))
        {
            var waterfallSteps = new WaterfallStep[]
            {
                FirstActStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> FirstActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Show card with repair types
            var response = MessageFactory.Attachment(await RepairTypeDetailsCard.CreateCardAttachmentAsync());
            await stepContext.Context.SendActivityAsync(response, cancellationToken);

            // End the dialog
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}