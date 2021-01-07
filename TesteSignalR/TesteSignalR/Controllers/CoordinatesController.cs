using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TesteSignalR.Models;
using TesteSignalR.Results;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TesteSignalR.Hubs;
using System;
using System.Security.Claims;

namespace TesteSignalR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoordinatesController : ControllerBase
    {
        private readonly static ConcurrentBag<StreamWriter> _clients = new ConcurrentBag<StreamWriter>();
        private readonly IHubContext<CoordinatesHub> _streaming;
        private readonly List<Coordinates> _items = new List<Coordinates>();

        public CoordinatesController(IHubContext<CoordinatesHub> streaming) => _streaming = streaming;

        [HttpGet]
        public ActionResult<List<Coordinates>> Get() => _items;

        [HttpGet]
        [Route("Get/{id}")]
        public ActionResult<Coordinates> Get(string id)
        {
            var item = _items.Find(item => item.UserId == id);
            if (item != null)
            {
                return item;
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<Coordinates>> Post([FromBody] Coordinates value)
        {
            if(value == null)
                return BadRequest();

            value.UserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            var current = value.UserId != null ? _items.Find(i => i.UserId == value.UserId) : null;
            if (current != null)
            {
                current.Lattitude = value.Lattitude;
                current.Longitude = value.Longitude;
            }
            else _items.Add(value);

            await WriteOnStream(value);

            return value;
        }

        // Métodos privados
        private async Task WriteOnStream(Coordinates data)
        {
            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            string jsonData = string.Format("{0}\n", JsonSerializer.Serialize(data, options));
            
            await _streaming.Clients.All.SendAsync("ReceiveMessage", jsonData);

            foreach (var client in _clients)
            {
                await client.WriteAsync(jsonData);
                await client.FlushAsync();
            }
        }
    }
}