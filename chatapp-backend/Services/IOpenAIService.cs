using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Services
{
    public interface IOpenAIService
    {
        Task<string> GetChatResponseAsync(string userMessage);
    }
}