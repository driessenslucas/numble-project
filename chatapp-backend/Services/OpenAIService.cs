using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using OpenAI.Chat;

namespace ChatApp.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIService> _logger;

        // Constructor accepts ILogger to enable logging
        public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _endpoint = configuration["Azure:OpenAIEndpoint"];
            
            // Await the asynchronous method here
            _apiKey = GetAzureKeyFromVault(configuration["Azure:KeyVaultUri"], 
                      configuration["Azure:OpenAIKeySecretName"]).Result;
            
            if (string.IsNullOrEmpty(_endpoint))
            {
                _logger.LogError("OpenAI endpoint is not configured.");
                throw new Exception("OpenAI endpoint is not configured.");
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("OpenAI API key is not configured.");
                throw new Exception("OpenAI API key is not configured.");
            }

            _logger.LogInformation($"OpenAI endpoint: {_endpoint}");
        }

        // Asynchronous method to get chat response from OpenAI
        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            try
            {
                AzureOpenAIClient client = new(new Uri(_endpoint), new AzureKeyCredential(_apiKey));

                Console.WriteLine("Client: ", client);
                var chatClient = client.GetChatClient("gpt-35-turbo");
                Console.WriteLine("chat Client: ", chatClient);
                var completionUpdates = chatClient.CompleteChatStreaming(
                    new SystemChatMessage("You are a helpful assistant."),
                    new UserChatMessage(userMessage)
                );

                string chatMessageContent = "";

                // Iterate through the updates to build the complete response
                foreach (var update in completionUpdates)
                {
                    foreach (var contentPart in update.ContentUpdate)
                    {
                        chatMessageContent += contentPart.Text;
                    }
                }

                return chatMessageContent;
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Failed to get chat response: {ex.Message}");
                _logger.LogError($"Failed to get chat response: {ex.Message}");
                return $"Failed to get message: {ex.Message}";
            }
        }

        // Asynchronous method to get API key from Azure KeyVault
        private async Task<string> GetAzureKeyFromVault(string keyVaultUri, string secretName)
        {
            try
            {
                var credential = new AzureCliCredential();
                var client = new SecretClient(new Uri(keyVaultUri), credential);

                // Use asynchronous method to fetch the secret
                var secret = await client.GetSecretAsync(secretName);

                if (secret == null)
                {
                    throw new Exception($"Secret '{secretName}' not found in KeyVault.");
                } 
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve secret: {ex.Message}", ex);
            }
            
        }
    }
}
