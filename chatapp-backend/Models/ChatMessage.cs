using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Models 
{
    public class ChatMessage
    {
        public required string MessageId { get; set; }     // Message ID
        public required string Text { get; set; }          // The message content
        public bool IsUserMessage { get; set; }   // True if user sent the message
        public required string Timestamp { get; set; }     // ISO 8601 timestamp
    }
}