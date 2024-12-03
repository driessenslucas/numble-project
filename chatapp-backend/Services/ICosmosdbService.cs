using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;
using ChatApp.Models;

namespace chatapp.Services
{
    public interface ICosmosDbService
    {
        Task SaveChatAsync(ChatHistory chatHistory);
        Task<ChatHistory[]> GetChatHistoryAsync(string userId);
    }
}