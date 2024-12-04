using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatApp.Models;

namespace ChatApp.Services
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponseAsync(string userMessage);

        Task<string> GetChatWithHistoryResponseAsync(string userMessage, ChatSession session);
    }
}