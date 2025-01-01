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

        private async Task<DialogTurnResult> LicensePlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve or initialize AppointmentDetails with embedded CustomerDetails
            var appointmentDetails = stepContext.Options as AppointmentDetails ?? new AppointmentDetails
            {
                Customer = new CustomerDetails()
            };

            // If license plate is missing, prompt for it
            if (string.IsNullOrEmpty(appointmentDetails.Customer.LicensePlate))
            {
                var promptMessage = MessageFactory.Text(LicensePlateStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(LicensePlateDialogID, new PromptOptions { Prompt = promptMessage, RetryPrompt = MessageFactory.Text("Please enter a valid license plate in the format '1-abc-123'.") }, cancellationToken);
            }

            // Proceed with the next step if license plate is already available or after license plate is entered
            return await stepContext.NextAsync(cancellationToken);
        }



        private async Task<DialogTurnResult> LicensePlateCheckStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve AppointmentDetails from stepContext.Options
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // Initialize Customer if not already initialized
            if (appointmentDetails.Customer == null)
            {
                appointmentDetails.Customer = new CustomerDetails();
            }

            // Save the license plate in CustomerDetails
            appointmentDetails.Customer.LicensePlate = (string)stepContext.Result;

            try
            {
                // Retrieve existing customer details
                var existingCustomer = await CustomerDataService.GetCustomerByLicenseplateAsync(appointmentDetails.Customer.LicensePlate);

                if (existingCustomer != null)
                {
                    // Map data to CustomerDetails
                    appointmentDetails.Customer.FirstName = existingCustomer.FirstName;
                    appointmentDetails.Customer.LastName = existingCustomer.LastName;
                    appointmentDetails.Customer.PhoneNumber = existingCustomer.PhoneNumber;
                    appointmentDetails.Customer.Mail = existingCustomer.Mail;

                    // Send customer details card
                    var customerDetailsCard = CustomerDetailsCard.CreateCardAttachment(appointmentDetails.Customer);
                    var cardActivity = MessageFactory.Attachment(customerDetailsCard);
                    await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

                    // Prompt for confirmation
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

                // If no existing customer, move on with appointmentDetails
                return await stepContext.NextAsync(appointmentDetails, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("We couldn't find your details. Let's proceed with registration."),
                    cancellationToken);

                stepContext.ActiveDialog.State["stepIndex"] = 2;

                // Pass the LicensePlate string to the next step to proceed with registration
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

                // Pass only the license plate to the next step
                return await stepContext.NextAsync(appointmentDetails.Customer.LicensePlate, cancellationToken); // Pass only the license plate
            }

            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text("Your registration has been canceled."),
                cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken); // End the dialog
        }


        private async Task<DialogTurnResult> FirstNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // If first name is missing, prompt for it
            if (string.IsNullOrEmpty(appointmentDetails.Customer.FirstName))
            {
                var promptMessage = MessageFactory.Text("Please provide your first name.", inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // Proceed to the next step, passing the appointmentDetails object
            return await stepContext.NextAsync(appointmentDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> LastNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // Save the first name from the previous step
            if (stepContext.Result is string firstNameResult)
            {
                appointmentDetails.Customer.FirstName = firstNameResult;
            }

            // If last name is missing, prompt for it
            if (string.IsNullOrEmpty(appointmentDetails.Customer.LastName))
            {
                var promptMessage = MessageFactory.Text(LastNameStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // Proceed to the next step with the existing last name
            return await stepContext.NextAsync(appointmentDetails.Customer.LastName, cancellationToken);
        }


        private async Task<DialogTurnResult> EmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // Save the last name from the previous step
            if (stepContext.Result is string lastNameResult)
            {
                appointmentDetails.Customer.LastName = lastNameResult;
            }

            // If email is missing, prompt for it
            if (string.IsNullOrEmpty(appointmentDetails.Customer.Mail))
            {
                var promptMessage = MessageFactory.Text(EmailStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(EmailDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // Proceed to the next step with the existing email
            return await stepContext.NextAsync(appointmentDetails.Customer.Mail, cancellationToken);
        }



        private async Task<DialogTurnResult> PhoneStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // Save the email from the previous step
            if (stepContext.Result is string emailResult)
            {
                appointmentDetails.Customer.Mail = emailResult;
            }

            // If phone number is missing, prompt for it
            if (string.IsNullOrEmpty(appointmentDetails.Customer.PhoneNumber))
            {
                var promptMessage = MessageFactory.Text(PhoneStepMsgText, inputHint: InputHints.ExpectingInput);
                return await stepContext.PromptAsync(PhoneDialogID, new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            // Proceed to the next step with the existing phone number
            return await stepContext.NextAsync(appointmentDetails.Customer.PhoneNumber, cancellationToken);
        }


        private async Task<DialogTurnResult> PhoneStepConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var appointmentDetails = stepContext.Options as AppointmentDetails;
            if (appointmentDetails == null)
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

            // Save the phone number directly on appointmentDetails.Customer
            appointmentDetails.Customer.PhoneNumber = (string)stepContext.Result;

            // Verify all customer details
            var customerDetails = appointmentDetails.Customer;
            if (string.IsNullOrEmpty(customerDetails.FirstName) ||
                string.IsNullOrEmpty(customerDetails.LastName) ||
                string.IsNullOrEmpty(customerDetails.Mail) ||
                string.IsNullOrEmpty(customerDetails.PhoneNumber) ||
                string.IsNullOrEmpty(customerDetails.LicensePlate))
            {
                throw new InvalidOperationException("Customer details are incomplete.");
            }

            // Create and send the confirmation card with all details
            var confirmationCard = CustomerDetailsCard.CreateCardAttachment(customerDetails);
            var cardActivity = MessageFactory.Attachment(confirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            // Define the yes/no choices for confirmation
            var yesnoList = new List<string> { "Confirm", "Cancel" };

            // Ask for confirmation
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

            // Get the user's choice
            var choice = stepContext.Result as FoundChoice;

            if (choice?.Value == "Confirm")
            {
                try
                {
                    // Save the customer details to the database
                    await CustomerDataService.InsertCustomerAsync(new Customer
                    {
                        FirstName = customerDetails.FirstName,
                        LastName = customerDetails.LastName,
                        Mail = customerDetails.Mail,
                        PhoneNumber = customerDetails.PhoneNumber,
                        LicensePlate = customerDetails.LicensePlate
                    });

                    // Notify the user of successful registration
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your information has been saved. Thank you!"), cancellationToken);

                    // Return the completed AppointmentDetails
                    return await stepContext.NextAsync(appointmentDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Handle any errors
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

            var repairTypes = await RepairTypeDataService.GetRepairTypesAsync();
            var selectedRepairType = repairTypes.FirstOrDefault(rt => rt.RepairName == selectedRepairTypeName);

            if (selectedRepairType != null)
            {
                var appointmentDetails = stepContext.Options as AppointmentDetails;
                if (appointmentDetails == null)
                    throw new InvalidCastException("stepContext.Options is not AppointmentDetails");

                appointmentDetails.RepairTypeId = selectedRepairType.RepairTypeId; // Update the correct RepairTypeId
                appointmentDetails.RepairType.RepairName = selectedRepairType.RepairName; // Save the name for confirmation display

                return await stepContext.NextAsync(appointmentDetails, cancellationToken);
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

                appointmentDetails.AppointmentDate = parsedDate.ToString(); // Update the date

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
            // Retrieve the appointment details from the options
            var appointmentDetails = (AppointmentDetails)stepContext.Options;

            if (appointmentDetails == null)
            {
                throw new InvalidCastException("stepContext.Options is not AppointmentDetails");
            }

            // Capture the selected time slot from the user's choice
            var selectedTimeSlot = (FoundChoice)stepContext.Result;

            // Parse the selected time slot into DateTime (assuming the format is valid)
            var selectedDateTime = DateTime.Parse(selectedTimeSlot.Value);

            // Convert the DateTime to a string that matches the desired time format
            string formattedTimeSlot = selectedDateTime.ToString("HH:mm"); // Format the time as "HH:mm" (24-hour format)

            // Update the TimeSlot property in appointmentDetails
            appointmentDetails.TimeSlot = new TimeSlot { StartTime = formattedTimeSlot }; // Set the StartTime as a formatted string

            // Log or debug the current state to check appointmentDetails
            Console.WriteLine($"AppointmentDetails: {JsonConvert.SerializeObject(appointmentDetails)}");

            // Create the confirmation card with the selected time slot
            var appointmentConfirmationCard = AppointmentDetailsCard.CreateCardAttachment(appointmentDetails);
            var cardActivity = MessageFactory.Attachment(appointmentConfirmationCard);
            await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

            // Prepare options for user confirmation
            var yesnoList = new List<string> { "Confirm", "Cancel" };

            // Prompt the user to confirm or cancel the appointment
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
                // Log de inhoud van appointmentDetails
                Console.WriteLine("Debugging appointmentDetails:");
                Console.WriteLine(JsonConvert.SerializeObject(appointmentDetails, Formatting.Indented));

                try
                {
                    // Haal de juiste TimeSlot en Customer op
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
                    // Maak een nieuwe klant (optioneel als deze al correct wordt opgehaald)
                    var customer = new Customer
                    {
                        CustomerId = rightCustomerInfo.CustomerId, // Gebruik de ID van rightCustomerInfo
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
                        TimeSlotId = rightTimeslot.TimeSlotId, // Gebruik de juiste TimeSlotId
                        RepairTypeId = rightRepairType.RepairTypeId,
                        CustomerId = rightCustomerInfo.CustomerId, // Gebruik de juiste CustomerId
                    };

                    // Log de inhoud van de afspraak voor debuggen
                    Console.WriteLine("Debugging appointment:");
                    Console.WriteLine(JsonConvert.SerializeObject(appointment, Formatting.Indented));

                    // Voeg de afspraak toe aan de database
                    await AppointmentDataService.InsertAppointmentAsync(appointment);

                    // Bevestig de succesvolle boeking
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("Your appointment has been successfully booked. Thank you!"),
                        cancellationToken);

                    return await stepContext.EndDialogAsync(appointmentDetails, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log eventuele fouten
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text($"There was an error saving your appointment: {ex.Message}"),
                        cancellationToken);

                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            else
            {
                // Annuleer de afspraak als de gebruiker dit aangeeft
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