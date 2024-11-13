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
            string systemMessage = "You are an intelligent assistant designed to provide informative and helpful responses to user queries. When a user asks a question or gives a command, generate a response that addresses their input clearly and accurately.\r\n\r\nExample Inputs and Outputs:\r\nuser: \"Can you help me with uploading a document?\"\r\nassistant: \"Sure, you can upload your document by following these steps...\"\r\n\r\nuser: \"How do I send an email through Outlook?\"\r\nassistant: \"To send an email through Outlook, you need to...\"\r\n\r\nuser: \"What should I do to report an IT issue?\"\r\nassistant: \"To report an IT issue, you can...\"\r\n\r\nNow, please provide an informative response to the following input:\r\n";
            try
            {
                OpenAIClient client = new(new Uri(_configuration["AzureOpenAIEndpoint"]), new AzureKeyCredential(_configuration["AzureOpenAIKey"]));
                ChatCompletionsOptions completionsOptions = new ChatCompletionsOptions()
                {
                    Messages = { new ChatMessage(ChatRole.System, systemMessage), new ChatMessage(ChatRole.User, userQuestion) },
                    Temperature = (float)0,
                    MaxTokens = 60, // Adjusted token limit
                    NucleusSamplingFactor = (float)1,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                Response<ChatCompletions> responseWithoutStream = await client.GetChatCompletionsAsync(_configuration["AzureOpenAIDeploymentNameGPT"], completionsOptions);
                ChatCompletions completions = responseWithoutStream.Value;
                return completions.Choices[0].Message.Content.Trim();
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Some Error Occurred in AzureOpenAIService.GetOpenAIResponse with error Message: {e.Message}");
                return null;
            }
        }
    }
}