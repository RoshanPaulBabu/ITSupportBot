using ITSupportBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ITSupportBot.Dialogs
{
    public class IntentHandlingDialog : ComponentDialog
    {
        private readonly AzureOpenAIService _AzureOpenAIService;

        public IntentHandlingDialog(AzureOpenAIService AzureOpenAIService) : base(nameof(IntentHandlingDialog))
        {
            _AzureOpenAIService = AzureOpenAIService;

            var waterfallSteps = new WaterfallStep[]
            {
                ExtractIntentAndMessageAsync,
                DisplayMessageAndSuggestionsAsync,
                HandleSuggestionActionAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ExtractIntentAndMessageAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string userQuestion = (string)stepContext.Options;

            string jsonResponse = await _AzureOpenAIService.GetOpenAIResponse(userQuestion);

            // Parse the JSON response to extract message and suggestions
            var responseData = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            stepContext.Values["message"] = responseData.GetProperty("message").GetString();

            // Convert suggestions to a list of strings for serialization compatibility
            var suggestions = new List<string>();
            foreach (var suggestion in responseData.GetProperty("suggestions").EnumerateArray())
            {
                suggestions.Add(suggestion.GetString());
            }
            stepContext.Values["suggestions"] = suggestions;

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayMessageAndSuggestionsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string message = (string)stepContext.Values["message"];
            var suggestions = (List<string>)stepContext.Values["suggestions"];

            List<CardAction> actions = new List<CardAction> { new CardAction(ActionTypes.ImBack, "Edit Message", "Edit Message") };
            

            foreach (var suggestion in suggestions)
            {
                actions.Add(new CardAction(ActionTypes.ImBack, suggestion, value: suggestion));
            }

            var reply = MessageFactory.SuggestedActions(actions, message, null, null);
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            return Dialog.EndOfTurn;
        }



        private async Task<DialogTurnResult> HandleSuggestionActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selectedAction = (string)stepContext.Result; // Get the selected suggestion from the user

            // Here we handle the action (for example, sending a confirmation message)
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"It has been successfully requested or sent: {selectedAction}."), cancellationToken);

            return await stepContext.PromptAsync(
               nameof(ConfirmPrompt),
               new PromptOptions { Prompt = MessageFactory.Text("Was this information helpful? (yes/no)") },
               cancellationToken);
        }

    }
}
