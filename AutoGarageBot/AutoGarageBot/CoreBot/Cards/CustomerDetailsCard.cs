using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using CoreBot.Models;
using System;
using CoreBot.DialogDetails;

namespace CoreBot.Cards
{
    public class CustomerDetailsCard
    {
        public static Attachment CreateCardAttachment(CustomerDetails customerDetails)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 3))
            {
                Body = new List<AdaptiveElement>
                {
                    // ColumnSet for logo and text next to each other
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            // Column for logo
                            new AdaptiveColumn
                            {
                                Width = "auto",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveImage
                                    {
                                        Url = new Uri("https://i.postimg.cc/VsGSB9p9/logo-speedyfix.png"),
                                        Size = AdaptiveImageSize.Small,  // Adjusted size to Small
                                        AltText = "SpeedyFix Logo"
                                    }
                                }
                            },
                            // Column for text
                            new AdaptiveColumn
                            {
                                Width = "stretch",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"Customer details for {customerDetails.FirstName} {customerDetails.LastName}",
                                        Weight = AdaptiveTextWeight.Bolder,
                                        Size = AdaptiveTextSize.Large
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Email**: {customerDetails.Mail}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Phone number**: {customerDetails.PhoneNumber}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**License plate**: {customerDetails.LicensePlate}",
                                        Wrap = true
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var adaptiveCardAttachment = new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JObject.FromObject(card)
            };

            return adaptiveCardAttachment;
        }
    }
}
