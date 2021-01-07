using TesteSignalR.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TesteSignalR.Hubs.Clients
{
    public interface ILogMessage
    {
        Task ReceiveMessage(LogMessage message);
    }
}
