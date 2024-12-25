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

namespace CoreBot.Dialogs
{
    public class CustomerInquiryDialog : CancelAndHelpDialog
    {
        private const string LicensePlateStepMsgText = "What is your license plate number?";
        private const string FirstNameStepMsgText = "What is your first name?";
        private const string LastNameStepMsgText = "What is your last name?";
        private const string EmailStepMsgText = "What is your email address?";
        private const string PhoneStepMsgText = "What is your phone number?";
        private const string ConfirmStepMsgText = "Is this information correct?";
        private const string RepairTypeStepMsgText = "What type of repair do you need?";

        private readonly string EmailDialogID = "EmailDialogID";
        private readonly string PhoneDialogID = "PhoneDialogID";

        public CustomerInquiryDialog()
            : base(nameof(CustomerInquiryDialog))
        {
            // Add necessary dialogs
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(EmailDialogID, EmailValidation));
            AddDialog(new TextPrompt(PhoneDialogID, PhoneValidation));
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
            };

            // Add waterfall dialog
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Step 1: Enter license plate number
        private async Task<DialogTurnResult> LicensePlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var customer = stepContext.Options as Customer ?? new Customer();

            if (string.IsNullOrEmpty(customer.LicensePlate))
            {
                var promptMessage = MessageFactory.Text(LicensePlateStepMsgText, LicensePlateStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(customer.LicensePlate, cancellationToken);
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
                    // Show confirmation card
                    var confirmationCard = CustomerDetailsCard.CreateCardAttachment(customer);
                    var cardActivity = MessageFactory.Attachment(confirmationCard);
                    await stepContext.Context.SendActivityAsync(cardActivity, cancellationToken);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(ConfirmStepMsgText), cancellationToken);

                    var yesnoList = new List<string> { "Confirm", "Cancel" };
                    var promptOptions = new PromptOptions
                    {
                        Choices = yesnoList.Select(choice => new Choice { Value = choice }).ToList(),
                        Prompt = MessageFactory.Text("Is this information correct?")
                    };

                    // Prompt user for confirmation (Yes/No choice)
                    return await stepContext.PromptAsync(nameof(ChoicePrompt), promptOptions, cancellationToken);
                }

                // If no customer found, throw an exception to proceed to registration
                throw new Exception("Customer not found");
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                Console.WriteLine($"Error occurred: {ex.Message}");

                // Proceed with new customer registration
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("We couldn't find your details. Let's proceed with registration."), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken); // Proceed to asking for registration details
            }
        }
        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userChoice = (string)stepContext.Result;

            if (userChoice == "Confirm")
            {
                // Proceed to the next step, for example, repair types selection
                return await stepContext.ReplaceDialogAsync(nameof(RepairTypeStepAsync), null, cancellationToken);
            }

            // If the user chooses "Cancel", reset or handle differently
            return await stepContext.ReplaceDialogAsync(nameof(LicensePlateCheckStepAsync), null, cancellationToken);
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
            var customer = (Customer)stepContext.Options;
            var choice = ((FoundChoice)stepContext.Result).Value;

            if (choice == "Confirm")
            {
                try
                {
                    await CustomerDataService.InsertCustomerAsync(customer);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your information has been saved. Thank you!"), cancellationToken);
                }
                catch (Exception ex)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"There was an error while saving your information: {ex.Message}"), cancellationToken);
                }

                return await stepContext.EndDialogAsync(customer, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your registration was canceled."), cancellationToken);
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
    }

}

