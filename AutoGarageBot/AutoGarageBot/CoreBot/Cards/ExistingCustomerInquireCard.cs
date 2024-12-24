using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CoreBot.Cards
{
    public class ExistingCustomerInquiryCard
    {
        public static Attachment CreateCardAttachment()
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "Are you an existing customer?",
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                        Spacing = AdaptiveSpacing.None
                    },
                    new AdaptiveActionSet
                    {
                        Actions = new List<AdaptiveAction>
                        {
                            new AdaptiveSubmitAction
                            {
                                Title = "Ja",
                                Data = new { Action = "ExistingCustomerYes" }
                            },
                            new AdaptiveSubmitAction
                            {
                                Title = "Nee",
                                Data = new { Action = "ExistingCustomerNo" }
                            }
                        }
                    }
                }
            };

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.FromObject(card)
            };

            return adaptiveCardAttachment;
        }
    }
}
