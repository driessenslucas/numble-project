using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using ChatApp.Models;

namespace ChatApp.Services 
{
    public interface ICosmosDbService
    {
        Task SaveSessionAsync(ChatSession session);
        Task<ChatSession> GetSessionAsync(string userId, string sessionId);
        Task<List<ChatSession>> GetSessionsForUserAsync(string userId);

        Task DeleteSessionAsync(string userId, string sessionId);
    }
}