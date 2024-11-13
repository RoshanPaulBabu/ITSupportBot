using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Threading;
using System.Threading.Tasks;
using ITSupportBot.Services;

namespace ITSupportBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly AzureOpenAIService _AzureOpenAIService;

        public MainDialog(UserState userState, AzureOpenAIService AzureOpenAIService)
            : base(nameof(MainDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _AzureOpenAIService = AzureOpenAIService;

            var waterfallSteps = new WaterfallStep[] {
                NameStepAsync,
                CallNumberDialogAsync,
                AskHelpQueryStepAsync,
                HandleUserQueryStepAsync,
                FinalStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberDialog());
            AddDialog(new TextPrompt("HelpQueryPrompt"));
            AddDialog(new IntentHandlingDialog(_AzureOpenAIService));
            

            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
        }

        private async Task<DialogTurnResult> CallNumberDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;

            // Call the NumberDialog to prompt for the user's number
            return await stepContext.BeginDialogAsync(nameof(NumberDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> AskHelpQueryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["number"] = (long)stepContext.Result;
            return await stepContext.PromptAsync("HelpQueryPrompt", new PromptOptions { Prompt = MessageFactory.Text("Hello! How can I help you today?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleUserQueryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string userQuestion = (string)stepContext.Result;

            // Call the IntentHandlingDialog with the user's question
            return await stepContext.BeginDialogAsync(nameof(IntentHandlingDialog), userQuestion, cancellationToken);
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Great! I'm glad I could help."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(MainDialog), null, cancellationToken);
            }
        }
    }
}
