using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TesteSignalR.Areas.Identity.Data;
using TesteSignalR.Hubs.Clients;
using TesteSignalR.Models;

namespace TesteSignalR.Hubs
{
    public class CoordinatesHub : Hub<ICoordinatesClient>
    {
        private readonly UserManager<User> _userManager;

        public CoordinatesHub(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task Update(Coordinates coordinates)
        {
            if (coordinates != null && !string.IsNullOrEmpty(coordinates.UserId))
            {
                if (Context.UserIdentifier != null)
                {
                    var name = await _userManager.GetUserNameAsync(await _userManager.GetUserAsync(Context.User));

                    var listeners = _userManager.Users
                        .Where(u => u.CoordinateTarget != null)
                        .Where(u => u.CoordinateTarget.UserName == name)
                        .Select(u => u.Id);

                    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                    string json = JsonSerializer.Serialize(coordinates, options);

                    await Clients.Users(listeners).ReceiveCoordinates(json);
                }
                else await Clients.Caller.ReceiveMessage("Usuário não está autenticado na API");
            }
            else await Clients.Caller.ReceiveMessage("Informe seu id, coordenadas, e seu alvo.");
        }
    }
}