using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Models;

public class CosmosDbService : ICosmosDbService
{
    private readonly Container _container;

    public CosmosDbService(CosmosClient client, string databaseName, string containerName)
    {
        _container = client.GetContainer(databaseName, containerName);
        // Get connection String from keyvault
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        session.LastUpdated = DateTime.UtcNow.ToString("o"); // Update the lastUpdated timestamp
        await _container.UpsertItemAsync(session, new PartitionKey(session.UserId));
    }

    public async Task<ChatSession> GetSessionAsync(string userId, string sessionId)
    {
        try
        {
            var response = await _container.ReadItemAsync<ChatSession>(sessionId, new PartitionKey(userId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<ChatSession>> GetSessionsForUserAsync(string userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var resultSet = _container.GetItemQueryIterator<ChatSession>(query);
        var sessions = new List<ChatSession>();

        while (resultSet.HasMoreResults)
        {
            var response = await resultSet.ReadNextAsync();
            sessions.AddRange(response);
        }

        return sessions;
    }
}
