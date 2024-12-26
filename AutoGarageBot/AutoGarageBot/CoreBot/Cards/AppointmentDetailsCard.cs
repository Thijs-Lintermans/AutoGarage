using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using CoreBot.Models;
using System;

namespace CoreBot.Cards
{
    public class AppointmentDetailsCard
    {
        public static Attachment CreateCardAttachment(Appointment appointment)
        {
            // Construct the adaptive card
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
                                        Size = AdaptiveImageSize.Small,
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
                                        Text = "Appointment Confirmation",
                                        Weight = AdaptiveTextWeight.Bolder,
                                        Size = AdaptiveTextSize.Large
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Customer**: {appointment.Customer?.FirstName} {appointment.Customer?.LastName}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Repair Type**: {appointment.RepairType?.RepairName}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Appointment Date**: {appointment.AppointmentDate:MMMM dd, yyyy}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Time Slot**: {appointment.TimeSlot?.StartTime:HH:mm}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**License Plate**: {appointment.Customer?.LicensePlate}",
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
