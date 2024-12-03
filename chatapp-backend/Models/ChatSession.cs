using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChatApp.Models
{
    public class ChatSession
    {
        [JsonProperty("id")]
        public required string sessionId { get; set; }             // Session ID
        
        [JsonProperty("userId")]
        public required string UserId { get; set; }        // User ID
        
        public required string SessionName { get; set; }   // Optional session name
        public List<ChatMessage> Messages { get; set; } = new(); // List of chat messages
        public required string LastUpdated { get; set; }   // ISO 8601 timestamp
    }
}