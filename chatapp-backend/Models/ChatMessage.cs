using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatapp.Models
{
    public class ChatMessage
    {
        public string MessageId { get; set; }     // Message ID
        public string Text { get; set; }          // The message content
        public bool IsUserMessage { get; set; }   // True if user sent the message
        public string Timestamp { get; set; }     // ISO 8601 timestamp
    }
}