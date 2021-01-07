using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using TesteSignalR.Hubs.Clients;
using TesteSignalR.Models;

namespace TesteSignalR.Hubs
{
    public class LogMessageHub : Hub<ILogMessage>
    {
        public async Task SendMessage(LogMessage message)
        {
            await Clients.All.ReceiveMessage(message);
        }
    }
}