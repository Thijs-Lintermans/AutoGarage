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
                AppointmentConfirmStepAsync,
                ConfirmAppointmentStepAsync
            };

            // Add waterfall dialog
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Step 1: Enter license plate number
        private async Task<DialogTurnResult> LicensePlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or create a CustomerDetails object for the dialog
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            var promptMessage = MessageFactory.Text(LicensePlateStepMsgText);
            var promptOptions = new PromptOptions
            {
                Prompt = promptMessage,
                RetryPrompt = MessageFactory.Text("Please enter a valid license plate in the format '1-abc-123'.")
            };

            return await stepContext.PromptAsync(LicensePlateDialogID, promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> LicensePlateCheckStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or create a CustomerDetails object for the dialog
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            // Save the entered license plate
            customerDetails.LicensePlate = (string)stepContext.Result;

            try
            {
                // Attempt to retrieve customer details from the database
                var existingCustomer = await CustomerDataService.GetCustomerByLicenseplateAsync(customerDetails.LicensePlate);

                if (existingCustomer != null)
                {
                    // Map existingCustomer (Customer) to CustomerDetails
                    customerDetails = new CustomerDetails
                    {
                        FirstName = existingCustomer.FirstName,
                        LastName = existingCustomer.LastName,
                        LicensePlate = existingCustomer.LicensePlate,
                        PhoneNumber = existingCustomer.PhoneNumber,
                        Mail = existingCustomer.Mail
                    };

                    // Create and send the details card
                    var customerDetailsCard = CustomerDetailsCard.CreateCardAttachment(customerDetails);
                    var cardActivity = MessageFactory.Attachment(customerDetailsCard);
                    await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

                    // Define yes/no choices and prepare prompt options
                    var yesnoList = new List<string> { "Confirm", "Cancel" };
                    var promptOptions = new PromptOptions
                    {
                        Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                        Prompt = MessageFactory.Text("") // Empty prompt, since the confirmation message was already shown
                    };

                    // Prompt for confirmation
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                }

                // If no customer is found, continue to registration
                throw new KeyNotFoundException("Customer not found");
            }
            catch (KeyNotFoundException)
            {
                // Notify the user and proceed to registration steps
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("We couldn't find your details. Let's proceed with registration."),
                    cancellationToken);

                // Continue dialog with updated CustomerDetails
                return await stepContext.NextAsync(customerDetails, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                Console.WriteLine($"Error occurred: {ex.Message}");

                // Notify the user about the issue
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("An unexpected error occurred. Please try again later."),
                    cancellationToken);

                // End the dialog in case of an error
                return await stepContext.EndDialogAsync(null, cancellationToken);
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
            // Retrieve or create CustomerDetails from the current dialog context
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            // Save the first name input from the user
            customerDetails.FirstName = (string)stepContext.Result;

            // If the last name is missing, prompt the user for it
            if (string.IsNullOrEmpty(customerDetails.LastName))
            {
                var promptMessage = MessageFactory.Text("Please provide your last name.");
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // If the last name is already filled, proceed to the next step
            return await stepContext.NextAsync(customerDetails, cancellationToken);
        }

        // Step 4: Last name
        private async Task<DialogTurnResult> LastNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or create CustomerDetails from the current dialog context
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            // Save the last name input from the user
            customerDetails.LastName = (string)stepContext.Result;

            // If the email is missing, prompt the user for it
            if (string.IsNullOrEmpty(customerDetails.Mail))
            {
                var promptMessage = MessageFactory.Text("Please provide your email address.");
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // If the email is already filled, proceed to the next step
            return await stepContext.NextAsync(customerDetails, cancellationToken);
        }


        // Step 5: Email
        // Step 5: Email address
        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or create CustomerDetails from the current dialog context
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            // Save the email input from the user
            customerDetails.Mail = (string)stepContext.Result;

            // If the phone number is missing, prompt the user for it
            if (string.IsNullOrEmpty(customerDetails.PhoneNumber))
            {
                var promptMessage = MessageFactory.Text("Please provide your phone number.");
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // If the phone number is already filled, proceed to the next step
            return await stepContext.NextAsync(customerDetails, cancellationToken);
        }

        // Step 6: Phone number
        // Step 6: Phone number
        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or create CustomerDetails from the current dialog context
            var customerDetails = stepContext.Options as CustomerDetails ?? new CustomerDetails();

            // Save the phone number input from the user
            customerDetails.PhoneNumber = (string)stepContext.Result;

            // Proceed to the next step (Phone number confirmation)
            return await stepContext.NextAsync(customerDetails, cancellationToken);
        }



        // Step 7: Confirm phone number
        // Step 7: Confirm phone number
        private async Task<DialogTurnResult> PhoneStepConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the CustomerDetails object passed from the previous step
            var customerDetails = (CustomerDetails)stepContext.Options;

            // Update the phone number in the CustomerDetails object with the value provided by the user
            customerDetails.PhoneNumber = (string)stepContext.Result;

            // Create and send the confirmation card for the details
            var confirmationCard = CustomerDetailsCard.CreateCardAttachment(customerDetails);
            var cardActivity = MessageFactory.Attachment(confirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            // Define the yes/no choices for the user to confirm or cancel
            var yesnoList = new List<string> { "Confirm", "Cancel" };

            // Prompt the user for confirmation via button clicks on the card
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                Prompt = MessageFactory.Text("Please confirm your details.")
            }, cancellationToken);
        }

        // Step 8: Final confirmation
        private async Task<DialogTurnResult> ConfirmDetailsStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the CustomerDetails object passed from the previous step
            var customerDetails = stepContext.Options as CustomerDetails;

            // Get the user's choice from the previous step
            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    // Create and save a new Customer using the details from CustomerDetails
                    await CustomerDataService.InsertCustomerAsync(new Customer
                    {
                        FirstName = customerDetails.FirstName,
                        LastName = customerDetails.LastName,
                        Mail = customerDetails.Mail,
                        PhoneNumber = customerDetails.PhoneNumber,
                        LicensePlate = customerDetails.LicensePlate
                    });

                    // Optionally store the confirmed details in the dialog's state
                    stepContext.ActiveDialog.State["selectedCustomer"] = customerDetails;

                    // Notify the user of the successful registration
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your information has been saved. Thank you!"), cancellationToken);

                    // Proceed to end the dialog with the customer details as the result
                    return await stepContext.EndDialogAsync(customerDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Handle any errors during the save process
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"There was an error saving your information: {ex.Message}"), cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                // Notify the user of the cancellation
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Your registration was canceled."), cancellationToken);

                return await stepContext.EndDialogAsync(null, cancellationToken);
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

        // Step for showing the confirmation card and asking the user to confirm the appointment
        private async Task<DialogTurnResult> AppointmentConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the details necessary for the appointment from the previous step
            var appointmentDetails = (AppointmentDetails)stepContext.Options;

            // Create and send the confirmation card for the appointment
            var appointmentConfirmationCard = AppointmentDetailsCard.CreateCardAttachment(appointmentDetails);
            var cardActivity = MessageFactory.Attachment(appointmentConfirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            // Define the yes/no choices for the user to confirm or cancel the appointment
            var yesnoList = new List<string> { "Confirm", "Cancel" };

            // Prompt the user for confirmation via button clicks on the card
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                Prompt = MessageFactory.Text("Please confirm your appointment details.")
            }, cancellationToken);
        }

        // Final step to confirm and save the appointment or cancel it
        private async Task<DialogTurnResult> ConfirmAppointmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the appointment details passed from the previous step
            var appointmentDetails = stepContext.Options as AppointmentDetails;

            // Get the user's choice from the previous step
            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    // Insert the appointment into the database via the API or data service
                    await AppointmentDataService.InsertAppointmentAsync(new Appointment
                    {
                        AppointmentDate = appointmentDetails.AppointmentDate,
                        TimeSlotId = appointmentDetails.TimeSlotId,
                        RepairTypeId = appointmentDetails.RepairTypeId,
                        CustomerId = appointmentDetails.CustomerId
                    });

                    // Notify the user that the appointment has been saved successfully
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your appointment has been successfully booked. Thank you!"), cancellationToken);

                    // Proceed to end the dialog with the appointment details as the result
                    return await stepContext.EndDialogAsync(appointmentDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Handle any errors during the save process
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"There was an error saving your appointment: {ex.Message}"), cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                // Notify the user of the cancellation
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Your appointment was canceled."), cancellationToken);

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