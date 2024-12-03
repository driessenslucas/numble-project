using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Models
{
    public class ChatRequest
    {
        public required string UserId { get; set; }
        public required string UserMessage { get; set; }
    }
}