using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ITSupportBot.Services
{
    public class AzureOpenAIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetOpenAIResponse(string userQuestion)
        {
            string systemMessage = "You are an intelligent assistant. Respond with JSON containing 'message' and a 'suggestions' array. Example: { \"message\": \"Here is the information you requested...\", \"suggestions\": [\"Forward to Finance\", \"Contact IT Support\", \"Send to HR\",\"Request Facilities Assistance\", \"Request Food and bverages\"] }.";

            try
            {
                OpenAIClient client = new(new Uri(_configuration["AzureOpenAIEndpoint"]), new AzureKeyCredential(_configuration["AzureOpenAIKey"]));
                ChatCompletionsOptions completionsOptions = new ChatCompletionsOptions()
                {
                    Messages = {
                new ChatMessage(ChatRole.System, systemMessage),
                new ChatMessage(ChatRole.User, "Example Input: Can you help me with payroll issues?"),
                new ChatMessage(ChatRole.Assistant, "{ \"message\": \"Please contact the Finance department for payroll assistance.\", \"suggestions\": \"Forward to Finance\"}"),
                new ChatMessage(ChatRole.User, userQuestion)
            },
                    Temperature = 0.0f,
                    MaxTokens = 150,
                    NucleusSamplingFactor = 1,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                Response<ChatCompletions> responseWithoutStream = await client.GetChatCompletionsAsync(_configuration["AzureOpenAIDeploymentNameGPT"], completionsOptions);
                ChatCompletions completions = responseWithoutStream.Value;
                return completions.Choices[0].Message.Content.Trim();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error in AzureOpenAIService.GetOpenAIResponse: {e.Message}");
                return null;
            }
        }


    }
}
