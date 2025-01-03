using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Helpers;
using CoreBot.Models;
using CoreBot.Cards;
using System.Linq;
using CoreBot.Services;
using Antlr4.Runtime.Misc;
using CoreBot.DialogDetails;
using Newtonsoft.Json;
using Microsoft.AspNetCore.JsonPatch.Internal;
using System.Security.AccessControl;

namespace CoreBot.Dialogs
{
    public class AppointmentDialog : CancelAndHelpDialog
    {
        private const string LicensePlateStepMsgText = "What is your license plate number?";
        private const string FirstNameStepMsgText = "What is your first name?";
        private const string LastNameStepMsgText = "What is your last name?";
        private const string EmailStepMsgText = "What is your email address?";
        private const string PhoneStepMsgText = "What is your phone number?";
        private const string ConfirmStepMsgText = "Is this information correct?";
        private const string RepairTypeStepMsgText = "What type of repair do you need?";
        private const string RepairDateStepMsgText = "When would you like to schedule your repair? Please provide a date (e.g., MM/DD/YYYY).";


        private readonly string EmailDialogID = "EmailDialogID";
        private readonly string PhoneDialogID = "PhoneDialogID";
        private readonly string DateDialogID = "DateDialogID";
        private readonly string LicensePlateDialogID = "LicensePlateDialogID";

        public AppointmentDialog()
            : base(nameof(AppointmentDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(EmailDialogID, EmailValidation));
            AddDialog(new TextPrompt(PhoneDialogID, PhoneValidation));
            AddDialog(new TextPrompt(DateDialogID, DateValidation));
            AddDialog(new TextPrompt(LicensePlateDialogID, LicensePlateValidation));

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            var waterfallSteps = new WaterfallStep[]
            {
                LicensePlateStepAsync,                
                LicensePlateCheckStepAsync,             
                ConfirmStepAsync,            
                FirstNameStepAsync,                      
                LastNameStepAsync,                       
                EmailStepAsync,                         
                PhoneStepAsync,                      
                PhoneStepConfirmAsync,                   
                ConfirmDetailsStepAsync,                 
                RepairTypeStepAsync,                    
                ProcessRepairTypeStepAsync,
                RepairDateStepAsync,                    
                GetTimeSlotsForDateStepAsync,            
                AppointmentConfirmStepAsync,
                ConfirmAppointmentStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> LicensePlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails ?? new AppointmentDetails
            {
                Customer = new CustomerDetails()
            };

            if (string.IsNullOrEmpty(appointmentDetails.Customer.LicensePlate))
            {
                var promptMessage = MessageFactory.Text(LicensePlateStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(LicensePlateDialogID, new PromptOptions { Prompt = promptMessage, RetryPrompt = MessageFactory.Text("Please enter a valid license plate in the format '1-abc-123'.") }, cancellationToken);
            }

            return await stepContext.NextAsync(cancellationToken);
        }



        private async Task<DialogTurnResult> LicensePlateCheckStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            if (appointmentDetails.Customer == null)
            {
                appointmentDetails.Customer = new CustomerDetails();
            }

            appointmentDetails.Customer.LicensePlate = (string)stepContext.Result;

            try
            {
                var existingCustomer = await CustomerDataService.GetCustomerByLicenseplateAsync(appointmentDetails.Customer.LicensePlate);

                if (existingCustomer != null)
                {
                    appointmentDetails.Customer.FirstName = existingCustomer.FirstName;
                    appointmentDetails.Customer.LastName = existingCustomer.LastName;
                    appointmentDetails.Customer.PhoneNumber = existingCustomer.PhoneNumber;
                    appointmentDetails.Customer.Mail = existingCustomer.Mail;

                    var customerDetailsCard = CustomerDetailsCard.CreateCardAttachment(appointmentDetails.Customer);
                    var cardActivity = MessageFactory.Attachment(customerDetailsCard);
                    await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

                    var promptOptions = new PromptOptions
                    {
                        Choices = new List<Choice>
                        {
                            new Choice("Confirm"),
                            new Choice("Cancel")
                        },
                        Prompt = MessageFactory.Text("Do you confirm these details?")
                    };

                    return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                }

                return await stepContext.NextAsync(appointmentDetails, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("We couldn't find your details. Let's proceed with registration."),
                    cancellationToken);

                stepContext.ActiveDialog.State["stepIndex"] = 2;

                return await stepContext.NextAsync(appointmentDetails.Customer.LicensePlate, cancellationToken);
            }
        }



        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userChoice = stepContext.Result as FoundChoice;

            var appointmentDetails = stepContext.Options as AppointmentDetails;

            if (userChoice != null && userChoice.Value == "Confirm")
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Your details have been confirmed. Let's proceed to select a repair type."),
                    cancellationToken);

                stepContext.ActiveDialog.State["stepIndex"] = 8;

                return await stepContext.NextAsync(appointmentDetails.Customer.LicensePlate, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Your registration has been canceled."),
                cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken); 
        }


        private async Task<DialogTurnResult> FirstNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            if (string.IsNullOrEmpty(appointmentDetails.Customer.FirstName))
            {
                var promptMessage = MessageFactory.Text("Please provide your first name.", inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(appointmentDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> LastNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            if (stepContext.Result is string firstNameResult)
            {
                appointmentDetails.Customer.FirstName = firstNameResult;
            }

            if (string.IsNullOrEmpty(appointmentDetails.Customer.LastName))
            {
                var promptMessage = MessageFactory.Text(LastNameStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(appointmentDetails.Customer.LastName, cancellationToken);
        }


        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            if (stepContext.Result is string lastNameResult)
            {
                appointmentDetails.Customer.LastName = lastNameResult;
            }

            if (string.IsNullOrEmpty(appointmentDetails.Customer.Mail))
            {
                var promptMessage = MessageFactory.Text(EmailStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(EmailDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(appointmentDetails.Customer.Mail, cancellationToken);
        }



        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            if (stepContext.Result is string emailResult)
            {
                appointmentDetails.Customer.Mail = emailResult;
            }

            if (string.IsNullOrEmpty(appointmentDetails.Customer.PhoneNumber))
            {
                var promptMessage = MessageFactory.Text(PhoneStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(PhoneDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(appointmentDetails.Customer.PhoneNumber, cancellationToken);
        }


        private async Task<DialogTurnResult> PhoneStepConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            appointmentDetails.Customer.PhoneNumber = (string)stepContext.Result;

            var customerDetails = appointmentDetails.Customer;
            if (string.IsNullOrEmpty(customerDetails.FirstName) ||
                string.IsNullOrEmpty(customerDetails.LastName) ||
                string.IsNullOrEmpty(customerDetails.Mail) ||
                string.IsNullOrEmpty(customerDetails.PhoneNumber) ||
                string.IsNullOrEmpty(customerDetails.LicensePlate))
            {
                throw new InvalidOperationException("Customer details are incomplete.");
            }

            var confirmationCard = CustomerDetailsCard.CreateCardAttachment(customerDetails);
            var cardActivity = MessageFactory.Attachment(confirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            var yesnoList = new List<string> { "Confirm", "Cancel" };

            var promptMessage = MessageFactory.Text("Please confirm your details:");
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                Prompt = promptMessage,
                Style = ListStyle.SuggestedAction
            }, cancellationToken);
        }



        private async Task<DialogTurnResult> ConfirmDetailsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            var customerDetails = appointmentDetails.Customer;

            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    await CustomerDataService.InsertCustomerAsync(new Customer
                    {
                        FirstName = customerDetails.FirstName,
                        LastName = customerDetails.LastName,
                        Mail = customerDetails.Mail,
                        PhoneNumber = customerDetails.PhoneNumber,
                        LicensePlate = customerDetails.LicensePlate
                    });

                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your information has been saved. Thank you!"), cancellationToken);

                    return await stepContext.NextAsync(appointmentDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"There was an error saving your information: {ex.Message}"), cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Your registration was canceled."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> RepairTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var repairTypes = await RepairTypeDataService.GetRepairTypesAsync();
            var choices = repairTypes.Select(rt => new Choice { Value = rt.RepairName }).ToList();

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text(RepairTypeStepMsgText),
                Choices = choices,
                RetryPrompt = MessageFactory.Text("Please select a valid repair type from the list."),
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ProcessRepairTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selectedRepairTypeName = (string)((FoundChoice)stepContext.Result).Value;

            var repairTypes = await RepairTypeDataService.GetRepairTypesAsync();
            var selectedRepairType = repairTypes.FirstOrDefault(rt => rt.RepairName == selectedRepairTypeName);

            if (selectedRepairType != null)
            {
                var appointmentDetails = stepContext.Options as AppointmentDetails;
                if (appointmentDetails == null)
                    throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

                appointmentDetails.RepairTypeId = selectedRepairType.RepairTypeId;
                appointmentDetails.RepairType.RepairName = selectedRepairType.RepairName;

                return await stepContext.NextAsync(appointmentDetails, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Invalid repair type selected.", cancellationToken: cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> RepairDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptMessage = MessageFactory.Text(RepairDateStepMsgText, RepairDateStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(DateDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> GetTimeSlotsForDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string userInput = (string)stepContext.Result;
            if (DateOnly.TryParse(userInput, out var parsedDate))
            {
                var appointmentDetails = stepContext.Options as AppointmentDetails;
                if (appointmentDetails == null)
                    throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

                appointmentDetails.AppointmentDate = parsedDate.ToString();

                var availableTimeSlots = await TimeSlotDataService.GetAvailableTimeSlotsByDateAsync(parsedDate);
                if (availableTimeSlots != null && availableTimeSlots.Any())
                {
                    var timeSlotChoices = availableTimeSlots.Select(ts => new Choice { Value = ts.StartTime.ToString() }).ToList();

                    return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                    {
                        Choices = timeSlotChoices,
                        Prompt = MessageFactory.Text("Please select an available time slot."),
                        RetryPrompt = MessageFactory.Text("Please select a valid time slot from the list.")
                    }, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("No available time slots for this date."), cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The date you entered is invalid. Please use the format MM/DD/YYYY."), cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken);
            }
        }


        private async Task<DialogTurnResult> AppointmentConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = (AppointmentDetails)stepContext.Options;

            if (appointmentDetails == null)
            {
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");
            }

            var selectedTimeSlot = (FoundChoice)stepContext.Result;

            var selectedDateTime = DateTime.Parse(selectedTimeSlot.Value);

            string formattedTimeSlot = selectedDateTime.ToString("HH:mm"); 

            appointmentDetails.TimeSlot = new TimeSlot { StartTime = formattedTimeSlot };

            Console.WriteLine($"AppointmentDetails: {JsonConvert.SerializeObject(appointmentDetails)}");

            var appointmentConfirmationCard = AppointmentDetailsCard.CreateCardAttachment(appointmentDetails);
            var cardActivity = MessageFactory.Attachment(appointmentConfirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            var yesnoList = new List<string> { "Confirm", "Cancel" };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                Prompt = MessageFactory.Text("Please confirm your appointment details.")
            }, cancellationToken);
        }




        private async Task<DialogTurnResult> ConfirmAppointmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    // Haal de juiste TimeSlot, Customer en RepairType op
                    var rightTimeslot = await TimeSlotDataService.GetTimeSlotByStartTimeAsync(appointmentDetails.TimeSlot.StartTime);
                    var rightCustomerInfo = await CustomerDataService.GetCustomerByLicenseplateAsync(appointmentDetails.Customer.LicensePlate);
                    var rightRepairType = await RepairTypeDataService.GetRepairTypeByIdAsync(appointmentDetails.RepairTypeId);

                    var strippedRepairType = new RepairType
                    {
                        RepairTypeId = rightRepairType.RepairTypeId,
                        RepairName = rightRepairType.RepairName,
                        RepairDescription = rightRepairType.RepairDescription
                    };
                    // Maak een nieuwe "gestripte" TimeSlot zonder gekoppelde afspraken
                    var strippedTimeslot = new TimeSlot
                    {
                        TimeSlotId = rightTimeslot.TimeSlotId,
                        StartTime = rightTimeslot.StartTime
                    };
                    // Maak een nieuwe klant
                    var customer = new Customer
                    {
                        CustomerId = rightCustomerInfo.CustomerId,
                        FirstName = rightCustomerInfo.FirstName,
                        LastName = rightCustomerInfo.LastName,
                        Mail = rightCustomerInfo.Mail,
                        PhoneNumber = rightCustomerInfo.PhoneNumber,
                        LicensePlate = rightCustomerInfo.LicensePlate
                    };

                    // Maak de afspraak aan
                    var appointment = new Appointment
                    {
                        AppointmentDate = appointmentDetails.AppointmentDate,
                        TimeSlotId = rightTimeslot.TimeSlotId,
                        RepairTypeId = rightRepairType.RepairTypeId,
                        CustomerId = rightCustomerInfo.CustomerId,
                    };


                    await AppointmentDataService.InsertAppointmentAsync(appointment);

                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your appointment has been successfully booked. Thank you!"),
                        cancellationToken);

                    return await stepContext.EndDialogAsync(appointmentDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"There was an error saving your appointment: {ex.Message}"),
                        cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Your appointment was canceled."),
                    cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }




        // Email validation
        private async Task<bool> EmailValidation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            const string EmailValidationError = "The email you entered is not valid, please enter a valid email.";

            string email = promptContext.Recognized.Value;
            if (Regex.IsMatch(email, @"^[\w\.-]+@[a-zA-Z\d\.-]+\.[a-zA-Z]{2,}$"))
            {
                return true;
            }
            await promptContext.Context.SendActivityAsync(EmailValidationError, cancellationToken: cancellationToken);
            return false;
        }

        // Phone validation
        private async Task<bool> PhoneValidation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            const string PhoneValidationError = "The phone number is not valid. Please use these formats: \"014 58 03 35\", \"0465 05 32 63\", \"+32 569 32 65 21\", \"+1 586 32 65 02\"";

            string number = promptContext.Recognized.Value;
            if (Regex.IsMatch(number, @"^(\+?\d{1,3} )?\d{3,4}( \d{2}){2,4}$"))
            {
                return true;
            }
            await promptContext.Context.SendActivityAsync(PhoneValidationError, cancellationToken: cancellationToken);
            return false;
        }

        //Date validation
        private async Task<bool> DateValidation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            const string DateValidationError = "The date you entered is not valid. Please use the format MM/DD/YYYY.";

            string dateInput = promptContext.Recognized.Value;
            if (DateTime.TryParseExact(dateInput, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out _))
            {
                return true;
            }
            await promptContext.Context.SendActivityAsync(DateValidationError, cancellationToken: cancellationToken);
            return false;
        }

        // License Plate validation
        private async Task<bool> LicensePlateValidation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            const string LicensePlateValidationError = "The license plate you entered is not valid. Please use the format '1-abc-123'.";

            string licensePlate = promptContext.Recognized.Value;
            if (Regex.IsMatch(licensePlate, @"^\d-[a-zA-Z]{3}-\d{3}$"))
            {
                return true;
            }
            await promptContext.Context.SendActivityAsync(LicensePlateValidationError, cancellationToken: cancellationToken);
            return false;
        }

    }

}