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
                        Text = "Please select a repair type from the list below:",
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Large,
                        Wrap = true
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "repairType",
                        Choices = new List<AdaptiveChoice>(),
                        Style = AdaptiveChoiceInputStyle.Compact, // Dropdown style
                        IsRequired = true
                    }
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "Submit",
                        Data = new { Action = "SelectRepairType" }
                    }
                }
            };

            // Populate dropdown choices
            var choiceSetInput = (AdaptiveChoiceSetInput)card.Body[1];
            foreach (var repairType in repairTypes)
            {
                choiceSetInput.Choices.Add(new AdaptiveChoice
                {
                    Title = repairType.RepairName,
                    Value = repairType.RepairName
                });
            }

            // Create the adaptive card attachment
            var adaptiveCardAttachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.FromObject(card)
            };

            return adaptiveCardAttachment;
        }
    }
}
