using System;

namespace TesteSignalR.Models
{
    public class LogMessage
    {
        public Guid Id { get; set; }
        public string Content { get; set; }

        public string Action { get; set; }
    }
}