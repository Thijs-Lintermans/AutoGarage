using AdaptiveCards;
using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using CoreBot.Models;
using System;
using CoreBot.DialogDetails;

namespace CoreBot.Cards
{
    public class AppointmentDetailsCard
    {
        public static Attachment CreateCardAttachment(AppointmentDetails appointmentDetails)
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
                                        Text = $"**Customer**: {appointmentDetails.Customer?.FirstName} {appointmentDetails.Customer?.LastName}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Repair Type**: {appointmentDetails.RepairType?.RepairName}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Appointment Date**: {appointmentDetails.AppointmentDate:MMMM dd, yyyy}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**Time Slot**: {appointmentDetails.TimeSlot?.StartTime:HH:mm}",
                                        Wrap = true
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = $"**License Plate**: {appointmentDetails.Customer?.LicensePlate}",
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
