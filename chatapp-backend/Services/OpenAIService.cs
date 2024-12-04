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
using ChatApp.Models;
using System.Linq;
using OpenAIChatMessage = OpenAI.Chat.ChatMessage; // since our model names overlap, we need to alias

namespace ChatApp.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;

        public OpenAIService(IConfiguration configuration)
        {
            try
            {
                _endpoint = configuration["Azure:OpenAIEndpoint"] ?? throw new ArgumentNullException("Azure:OpenAIEndpoint is not configured.");
                _apiKey = GetAzureKeyFromVault(configuration["Azure:KeyVaultUri"] ?? throw new ArgumentNullException("KeyVaultUri is null"), 
                                            configuration["Azure:OpenAIKeySecretName"] ?? throw new ArgumentNullException("OpenAIKeySecretName is null")).Result;

                if (string.IsNullOrEmpty(_endpoint))
                {
                    Console.WriteLine("OpenAI endpoint is not configured.");
                    throw new Exception("OpenAI endpoint is not configured.");
                }

                if (string.IsNullOrEmpty(_apiKey))
                {
                    Console.WriteLine("OpenAI API key is not configured.");
                    throw new Exception("OpenAI API key is not configured.");
                }

                Console.WriteLine($"OpenAI endpoint: {_endpoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize OpenAI service: {ex.Message}");
                throw new Exception("Failed to initialize OpenAI service.");
            }
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
                
                Console.WriteLine($"Failed to get chat response: {ex.Message}");
                return $"Failed to get message: {ex.Message}";
            }
        }

        public async Task<string> GetChatWithHistoryResponseAsync(string userMessage, ChatSession session)
        {
            try
            {
                AzureOpenAIClient client = new(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
                var chatClient = client.GetChatClient("gpt-35-turbo");

                var sessionMessages = session.Messages.Select(m =>
                    m.IsUserMessage
                        ? (OpenAIChatMessage)new OpenAI.Chat.UserChatMessage(m.Text)
                        : new OpenAI.Chat.AssistantChatMessage(m.Text)).ToList();

                var chatHistory = new List<OpenAIChatMessage>
                {
                    new OpenAI.Chat.SystemChatMessage("You are a helpful assistant.")
                };
                chatHistory.AddRange(sessionMessages);
                chatHistory.Add(new OpenAI.Chat.UserChatMessage(userMessage));

                var completionUpdates = chatClient.CompleteChatStreaming(chatHistory);

                string chatMessageContent = "";
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
                Console.WriteLine($"Failed to get chat response: {ex.Message}");
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

                if (secret == null || string.IsNullOrEmpty(secret.Value?.Value))
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
