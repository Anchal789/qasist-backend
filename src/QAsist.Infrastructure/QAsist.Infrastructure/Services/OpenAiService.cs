using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using QAsist.Application.Interfaces.IServices;
using System.Text.Json;

namespace QAsist.Infrastructure.Services
{
    public class OpenAiService : IAiService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAiService> _logger;
        private readonly ChatClient _chatClient;

        public OpenAiService(IConfiguration configuration, ILogger<OpenAiService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI API Key not configured");

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o";

            _chatClient = new ChatClient(model, apiKey);
        }

        public async Task<string> GenerateCompletionAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

                var options = new ChatCompletionOptions
                {
                    Temperature = float.Parse(_configuration["OpenAI:Temperature"] ?? "0.2"),
                    MaxOutputTokenCount = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "4000")
                };

                var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

                var content = response.Value.Content[0].Text;

                _logger.LogInformation(
                    "OpenAI completion generated. Tokens used: {TokensUsed}",
                    response.Value.Usage.TotalTokenCount);

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                throw;
            }
        }

        public async Task<T> GenerateJsonResponseAsync<T>(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

                var options = new ChatCompletionOptions
                {
                    Temperature = float.Parse(_configuration["OpenAI:Temperature"] ?? "0.2"),
                    MaxOutputTokenCount = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "4000"),
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                var response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

                var jsonContent = response.Value.Content[0].Text;

                _logger.LogInformation(
                    "OpenAI JSON completion generated. Tokens used: {TokensUsed}",
                    response.Value.Usage.TotalTokenCount);

                var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? throw new InvalidOperationException("Failed to deserialize AI response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API for JSON response");
                throw;
            }
        }
    }
}
