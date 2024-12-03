using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatapp.Models
{

    public class ChatSession
    {
        public string Id { get; set; }             // Session ID
        public string UserId { get; set; }        // User ID
        public string SessionName { get; set; }   // Optional session name
        public List<ChatMessage> Messages { get; set; } = new(); // List of chat messages
        public string LastUpdated { get; set; }   // ISO 8601 timestamp
    }
}