using Microsoft.AspNetCore.SignalR;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using TesteSignalR.Hubs.Clients;
using TesteSignalR.Models;

namespace TesteSignalR.Hubs
{
    public class CoordinatesHub : Hub<ICoordinatesClient>
    {
        public Task Update(Coordinates value)
        {
            if (value == null || string.IsNullOrEmpty(value.UserId))
                return Clients.Caller.ReceiveMessage("Informe seu id, coordenadas, e seu alvo.");
            //if (Context.UserIdentifier == null)
            //    return Clients.Caller.ReceiveMessage(("Usuário não está auntenticado na API");

            if (Context.Items.ContainsKey(value.UserId))
            {
                Context.Items[value.UserId] = value;
            } else Context.Items.Add(value.UserId, value);

            if (Context.Items.ContainsKey(value.TargetId))
            {
                var result = Context.Items[value.TargetId];
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                string json = JsonSerializer.Serialize(result, options);
                return Clients.All.ReceiveCoordinates(json);
            }
            return Clients.Caller.ReceiveMessage("Não foi possível receber as coordenadas de " + value.TargetId);
        }
    }
}