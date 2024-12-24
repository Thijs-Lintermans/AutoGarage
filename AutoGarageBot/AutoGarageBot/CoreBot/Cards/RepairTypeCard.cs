using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using CoreBot.Models;
using System.Threading.Tasks;

namespace CoreBot.Cards
{
    public class RepairTypeCard
    {
        public static async Task<Attachment> CreateCardAttachmentAsync()
        {
            // Fetch repair types from the API
            var repairTypes = await RepairTypeDataService.GetRepairTypesAsync();

            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 3))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "Available Repair Types",
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large
                    }
                }
            };

            // Add a text block for each repair type
            foreach (var repairType in repairTypes)
            {
                card.Body.Add(new AdaptiveTextBlock
                {
                    Text = $"**{repairType.RepairName}**: {repairType.RepairDescription}",
                    Wrap = true
                });
            }

            // Create the attachment without actions
            var adaptiveCardAttachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.FromObject(card)
            };

            return adaptiveCardAttachment;
        }
    }
}
