using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using CoreBot.Helpers;
using Antlr4.Runtime.Misc;

namespace CoreBot.Dialogs
{
    public class OpeningHoursDialog : CancelAndHelpDialog
    {
        public OpeningHoursDialog()
            : base(nameof(OpeningHoursDialog))
        {

            var waterfallSteps = new WaterfallStep[]
            {
                FirstActStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> FirstActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var response = MessageFactory.Attachment(CardHelper.CreateCardAttachment("openingHoursCard"));
            await stepContext.Context.SendActivityAsync(response, cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
