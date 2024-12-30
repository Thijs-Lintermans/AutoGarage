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
            // Add necessary dialogs
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(EmailDialogID, EmailValidation));
            AddDialog(new TextPrompt(PhoneDialogID, PhoneValidation));
            AddDialog(new TextPrompt(DateDialogID, DateValidation));
            AddDialog(new TextPrompt(LicensePlateDialogID, LicensePlateValidation));

            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            // Define waterfall steps
            var waterfallSteps = new WaterfallStep[]
            {
                LicensePlateStepAsync,                   // Step 1: License plate number
                LicensePlateCheckStepAsync,              // Step 2: Check if customer exists by license plate
                ConfirmStepAsync,            // Step 3: If customer exists, confirm their details
                FirstNameStepAsync,                      // Step 4: Ask for first name
                LastNameStepAsync,                       // Step 5: Ask for last name
                EmailStepAsync,                          // Step 6: Ask for email
                PhoneStepAsync,                          // Step 7: Ask for phone number
                PhoneStepConfirmAsync,                   // Step 8: Confirm phone number
                ConfirmDetailsStepAsync,                 // Step 9: Confirm details after entering new customer info
                RepairTypeStepAsync,                     // Step 10: Repair type
                ProcessRepairTypeStepAsync,
                RepairDateStepAsync,                     // Step 11: Ask for repair date
                GetTimeSlotsForDateStepAsync,            // Step 12: Get available time slots for the selected date
                TimeSlotSelectionStepAsync
            };

            // Add waterfall dialog
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Step 1: Enter license plate number
        private async Task<DialogTurnResult> LicensePlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();

            customer.LastName = (string)stepContext.Result;  // Example assignment, you can adjust this.

            var promptMessage = MessageFactory.Text(LicensePlateStepMsgText);
            var promptOptions = new PromptOptions
            {
                Prompt = promptMessage,
                RetryPrompt = MessageFactory.Text("Please enter a valid license plate in the format '1-abc-123'.")
            };

            // Add the custom validator to the prompt options
            var validator = new PromptValidator<string>((promptContext, ct) => LicensePlateValidation(promptContext, ct));

            return await stepContext.PromptAsync(LicensePlateDialogID, promptOptions, cancellationToken);  // Use the correct ID here
        }




        private async Task<DialogTurnResult> LicensePlateCheckStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customerInquiryDetails = stepContext.Options as Customer ?? new Customer();
            customerInquiryDetails.LicensePlate = (string)stepContext.Result;

            try
            {
                // Check if the customer exists in your database
                var customer = await CustomerDataService.GetCustomerByLicenseplateAsync(customerInquiryDetails.LicensePlate);

                // If customer exists, display their details
                if (customer != null)
                {
                    // Store the selected customer in the dialog state
                    stepContext.ActiveDialog.State["selectedCustomer"] = customer;

                    // Create and send the details card
                    var customerDetailsCard = CustomerDetailsCard.CreateCardAttachment(customer);
                    var cardActivity = MessageFactory.Attachment(customerDetailsCard);
                    await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

                    // Show the confirmation message
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("We found your details! Please confirm."), cancellationToken);

                    // Prepare the prompt with only the choices, without repeating the confirmation message
                    var yesnoList = new List<string> { "Confirm", "Cancel" };
                    var promptOptions = new PromptOptions
                    {
                        Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                        Prompt = MessageFactory.Text("") // Empty prompt, since the confirmation message was already shown
                    };

                    // Prompt for confirmation without repeating the message
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                }

                // If no customer found, proceed with registration
                throw new Exception("Customer not found");
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error occurred: {ex.Message}");

                // Proceed with new customer registration
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("We couldn't find your details. Let's proceed with registration."), cancellationToken);

                stepContext.ActiveDialog.State["stepIndex"] = 2;

                // Skip directly to the step by continuing the dialog
                return await stepContext.ContinueDialogAsync(cancellationToken);
            }
        }



        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Ensure the result is a FoundChoice object
            var userChoice = stepContext.Result as FoundChoice;

            if (userChoice != null && userChoice.Value == "Confirm")
            {
                // Set the stepIndex to the desired step (e.g., step 7 for RepairTypeStepAsync)
                stepContext.ActiveDialog.State["stepIndex"] = 8;

                // Skip directly to the step by continuing the dialog
                return await stepContext.ContinueDialogAsync(cancellationToken);
            }

            // If the user chooses "Cancel", stop the dialog
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your registration was canceled."), cancellationToken);

            // End the dialog when user presses cancel
            return await stepContext.EndDialogAsync(cancellationToken); // This will stop the dialog
        }




        // Step 3: New customer details (First Name)
        private async Task<DialogTurnResult> FirstNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();

            var promptMessage = MessageFactory.Text(FirstNameStepMsgText);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        // Step 4: Last name
        private async Task<DialogTurnResult> LastNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();
            customer.FirstName = (string)stepContext.Result;

            var promptMessage = MessageFactory.Text(LastNameStepMsgText);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        // Step 5: Email
        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();
            customer.LastName = (string)stepContext.Result;

            var promptMessage = MessageFactory.Text(EmailStepMsgText);
            return await stepContext.PromptAsync(EmailDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        // Step 6: Phone number
        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();
            customer.Mail = (string)stepContext.Result;

            var promptMessage = MessageFactory.Text(PhoneStepMsgText);
            return await stepContext.PromptAsync(PhoneDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        // Step 7: Confirm phone number
        private async Task<DialogTurnResult> PhoneStepConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = (Customer)stepContext.Options;
            customer.PhoneNumber = (string)stepContext.Result;

            // Create and send the confirmation card for the details
            var confirmationCard = CustomerDetailsCard.CreateCardAttachment(customer);
            var cardActivity = MessageFactory.Attachment(confirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            // Define the yes/no choices
            var yesnoList = new List<string> { "Confirm", "Cancel" };

            // Prompt for user confirmation via button clicks on the card
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                Prompt = MessageFactory.Text("Please confirm your details.")
            }, cancellationToken);
        }

        // Step 8: Final confirmation
        private async Task<DialogTurnResult> ConfirmDetailsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();
            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    // Save the new customer to the database
                    await CustomerDataService.InsertCustomerAsync(customer);

                    // Set selectedCustomer for new customer
                    stepContext.ActiveDialog.State["selectedCustomer"] = customer;

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your information has been saved. Thank you!"), cancellationToken);
                    return await stepContext.NextAsync(null, cancellationToken);
                }
                catch (Exception ex)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"There was an error saving your information: {ex.Message}"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your registration was canceled."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken);
            }
        }




        // Step 9: Repair type selection
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

            // Fetch the repair types again
            var repairTypes = await RepairTypeDataService.GetRepairTypesAsync();

            // Find the selected repair type
            var selectedRepairType = repairTypes.FirstOrDefault(rt => rt.RepairName == selectedRepairTypeName);

            if (selectedRepairType != null)
            {
                // Store the selected repair type in the dialog state
                stepContext.ActiveDialog.State["selectedRepairTypeId"] = selectedRepairType.RepairTypeId;
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Invalid repair type selected.", cancellationToken: cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken);
            }
        }



        // Step 10: Ask for repair date
        private async Task<DialogTurnResult> RepairDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt the user for a date
            var promptMessage = MessageFactory.Text(RepairDateStepMsgText, RepairDateStepMsgText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        private async Task<DialogTurnResult> GetTimeSlotsForDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string userInput = (string)stepContext.Result;
            if (DateOnly.TryParse(userInput, out var parsedDate))
            {
                try
                {
                    // Fetch available time slots for the parsed date
                    var availableTimeSlots = await TimeSlotDataService.GetAvailableTimeSlotsByDateAsync(parsedDate);

                    if (availableTimeSlots != null && availableTimeSlots.Any())
                    {
                        // Store the StartTime values in the dialog state
                        var timeSlotStartTimes = availableTimeSlots.Select(ts => ts.StartTime.ToString()).ToList();
                        stepContext.Values["availableTimeSlotStartTimes"] = timeSlotStartTimes;

                        // Create the list of choices (displaying the time slot start time)
                        var timeSlotChoices = availableTimeSlots.Select(ts => new Choice { Value = ts.StartTime.ToString() }).ToList();

                        // Create the prompt message
                        var promptMessage = MessageFactory.Text("Please select an available time slot.");

                        // Create PromptOptions with the list of choices
                        var promptOptions = new PromptOptions
                        {
                            Choices = timeSlotChoices,
                            Prompt = promptMessage,
                            RetryPrompt = MessageFactory.Text("Please select a valid time slot from the list.")
                        };

                        // Prompt the user to select a time slot
                        return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                    }
                    else
                    {
                        // Handle the case when no time slots are available
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("No available time slots for this date."), cancellationToken);
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    // Handle error fetching time slots
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"An error occurred while fetching time slots: {ex.Message}"), cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                // Handle invalid date format
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The date you entered is invalid. Please use the format MM/DD/YYYY."), cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken); // Restart the dialog
            }
        }

        private async Task<DialogTurnResult> TimeSlotSelectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedTimeSlotString = ((FoundChoice)stepContext.Result).Value;
            var availableTimeSlotStartTimes = (List<string>)stepContext.Values["availableTimeSlotStartTimes"];

            if (availableTimeSlotStartTimes.Contains(selectedTimeSlotString))
            {
                var selectedTimeSlot = await TimeSlotDataService.GetTimeSlotByStartTimeAsync(selectedTimeSlotString);

                if (selectedTimeSlot != null)
                {
                    stepContext.Values["selectedTimeSlotId"] = selectedTimeSlot.TimeSlotId;

                    var selectedRepairTypeId = (int)stepContext.ActiveDialog.State["selectedRepairTypeId"];
                    var selectedRepairType = await RepairTypeDataService.GetRepairTypeByIdAsync(selectedRepairTypeId);

                    // Retrieve selectedCustomer (either new or existing)
                    var selectedCustomer = (Customer)stepContext.ActiveDialog.State["selectedCustomer"];

                    if (selectedCustomer == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("No customer found for the appointment."), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken);
                    }

                    // Create an appointment with selected details
                    var appointment = new Appointment
                    {
                        AppointmentDate = DateTime.Now,
                        TimeSlotId = selectedTimeSlot.TimeSlotId,
                        RepairTypeId = selectedRepairType.RepairTypeId,
                        CustomerId = selectedCustomer.CustomerId,
                        TimeSlot = selectedTimeSlot,
                        RepairType = selectedRepairType,
                        Customer = selectedCustomer
                    };

                    // Create and send appointment confirmation card
                    var appointmentCard = AppointmentDetailsCard.CreateCardAttachment(appointment);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(appointmentCard), cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Invalid time slot selected. Please try again."), cancellationToken);
                    return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("The selected time slot is no longer available. Please try again."), cancellationToken);
                return await stepContext.ReplaceDialogAsync(nameof(AppointmentDialog), null, cancellationToken);
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