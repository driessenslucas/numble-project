using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatApp.Models;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Internal;

namespace ChatApp.Services;
public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(IConfiguration configuration)
    {
        try
        {
            var keyVaultUri = configuration["Azure:KeyVaultUri"] ?? throw new ArgumentNullException("KeyVaultUri is not configured.");
            var secretName = configuration["Azure:CosmosDbConnectionString"] ?? throw new ArgumentNullException("CosmosDbConnectionString is not configured.");
            
            Console.WriteLine($"KeyVaultUri: {keyVaultUri}");
            Console.WriteLine($"SecretName: {secretName}");

            var credential = new AzureCliCredential();
            var secretClient = new SecretClient(new Uri(keyVaultUri), credential);

            // Async retrieval of the secret
            Console.WriteLine("Retrieving secret...");
            var secretResponse = secretClient.GetSecretAsync(secretName).Result;
            var connectionString = secretResponse.Value.Value;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Failed to retrieve Cosmos DB connection string from Key Vault");
            }

            Console.WriteLine("Connection string retrieved successfully.");
            Console.WriteLine($"Connection string: {connectionString}");

            // Initialize CosmosClient with the retrieved connection string
            var cosmosClient = new CosmosClient(connectionString);
            
            // Get database and container names from configuration
            var databaseName = configuration["CosmosDb:DatabaseName"] ?? throw new ArgumentNullException("DatabaseName is not configured.");
            var containerName = configuration["CosmosDb:ContainerName"] ?? throw new ArgumentNullException("ContainerName is not configured.");

            Console.WriteLine($"DatabaseName: {databaseName}");
            Console.WriteLine($"ContainerName: {containerName}");

            try
            {
                // Create database if it doesn't exist
                Console.WriteLine("Creating database if it doesn't exist...");
                var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).Result;
                var database = databaseResponse.Database;

                // Create container if it doesn't exist with partition key
                Console.WriteLine($"Creating container if it doesn't exist with partition key path: /userId");
                var containerProperties = new ContainerProperties(containerName, partitionKeyPath: "/userId");
                var containerResponse = database.CreateContainerIfNotExistsAsync(containerProperties).Result;
                Console.WriteLine($"Container created successfully.");
                Console.WriteLine($"Container ID: {containerResponse.Container.Id}");
                Console.WriteLine($"Container Partition Key Path: {containerProperties.PartitionKeyPath}");
                _container = containerResponse.Container;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"CosmosException during database or container creation: {ex.Message}");
                throw new Exception($"Failed to create database or container: {ex.Message}", ex);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CosmosDbService constructor: {ex.Message}");
            throw;
        }
    }



    public async Task SaveSessionAsync(ChatSession session)
    {
        try
        {
            Console.WriteLine($"Attempting to save session {session.sessionId} for user {session.UserId}");
            
            if (string.IsNullOrEmpty(session.sessionId))
            {
                throw new ArgumentException("sessionId cannot be null or empty");
            }
            if (string.IsNullOrEmpty(session.UserId))
            {
                throw new ArgumentException("UserId cannot be null or empty");
            }

            // Set SessionName based on the first two words of the first user message
            if (session.Messages != null && session.Messages.Count > 0)
            {
                var firstUserMessage = session.Messages.FirstOrDefault(m => m.IsUserMessage)?.Text;
                if (!string.IsNullOrEmpty(firstUserMessage))
                {
                    var words = firstUserMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    session.SessionName = string.Join(" ", words.Take(2)) + (words.Length > 2 ? "..." : "");
                }
            }

            session.LastUpdated = DateTime.UtcNow.ToString("o");
            
            var response = await _container.UpsertItemAsync(
                item: session,
                partitionKey: new PartitionKey(session.UserId)
            );

            Console.WriteLine($"Successfully saved session. RequestCharge: {response.RequestCharge} RU/s");
        }
        catch (CosmosException ex)
        {
            Console.Error.WriteLine($"Cosmos DB Error saving session: {ex.GetType().Name} - {ex.Message}");
            Console.Error.WriteLine($"Status Code: {ex.StatusCode}, Sub Status Code: {ex.SubStatusCode}");
            Console.Error.WriteLine($"Activity Id: {ex.ActivityId}");
            Console.Error.WriteLine($"Request Charge: {ex.RequestCharge} RU/s");
            throw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error saving session: {ex.GetType().Name} - {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<ChatSession> GetSessionAsync(string userId, string sessionId)
    {
        try
        {
            var response = await _container.ReadItemAsync<ChatSession>(
                id: sessionId, 
                partitionKey: new PartitionKey(userId)
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Session not found. UserId: {userId}, SessionId: {sessionId}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving session. UserId: {userId}, SessionId: {sessionId}, Error: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ChatSession>> GetSessionsForUserAsync(string userId)
    {
        try
        {
            Console.WriteLine($"Retrieving sessions for user {userId}");
            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var resultSet = _container.GetItemQueryIterator<ChatSession>(query);
            var sessions = new List<ChatSession>();

            while (resultSet.HasMoreResults)
            {
                var response = await resultSet.ReadNextAsync();
                sessions.AddRange(response);
            }

            Console.WriteLine($"Retrieved {sessions.Count} sessions for user {userId}.");
            return sessions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving sessions for user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteSessionAsync(string userId, string sessionId)
    {
        try
        {
            Console.WriteLine($"Deleting session {sessionId} for user {userId}");
            var response = await _container.DeleteItemAsync<ChatSession>(sessionId, new PartitionKey(userId));
            Console.WriteLine($"Session deleted. RequestCharge: {response.RequestCharge} RU/s");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine($"Session not found: {ex.Message}");
            throw new InvalidOperationException("Session not found.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting session: {ex.GetType().Name} - {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
