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

namespace TesteSignalR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private static List<Item> _itens = new List<Item>();
        private static ConcurrentBag<StreamWriter> _clients = new ConcurrentBag<StreamWriter>();
        private readonly IHubContext<StreamingHub> _streaming;

        public TodoController(IHubContext<StreamingHub> streaming) => _streaming = streaming;

        [HttpGet]
        public ActionResult<List<Item>> Get() => _itens;

        [HttpGet]
        [Route("streaming")]
        public IActionResult Streaming()
        {
            return new StreamResult(
                (stream, cancelToken) => {
                    var wait = cancelToken.WaitHandle;
                    var client = new StreamWriter(stream);
                    _clients.Add(client);

                    wait.WaitOne();

                    StreamWriter ignore;
                    _clients.TryTake(out ignore);
                }, 
                HttpContext.RequestAborted);
        }

        [HttpPost]
        public async Task<ActionResult<Item>> Post([FromBody] Item value)
        {
            if(value == null)
                return BadRequest();

            if(value.Id == 0)
            {
                var max = _itens.Any() ? _itens.Max(i => i.Id) : 0;
                value.Id = max+1;
            }

            _itens.Add(value);

            await WriteOnStream(value, "Item added");

            return value;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Item>> Put(long id, [FromBody] Item value)
        {
            var item = _itens.SingleOrDefault(i => i.Id == id);
            if(item != null)
            {
                _itens.Remove(item);
                value.Id = id;
                _itens.Add(value);

                await WriteOnStream(value, "Item updated");

                return item;
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var item = _itens.SingleOrDefault(i => i.Id == id);
            if(item != null)
            {
                _itens.Remove(item);
                await WriteOnStream(item, "Item removed");
                return Ok(new { Description = "Item removed" });
            }

            return BadRequest();
        }

        // Métodos privados
        private async Task WriteOnStream(Item data, string action)
        {
            string jsonData = string.Format("{0}\n", JsonSerializer.Serialize(new { data, action }));

            await _streaming.Clients.All.SendAsync("ReceiveMessage", jsonData);

            foreach (var client in _clients)
            {
                await client.WriteAsync(jsonData);
                await client.FlushAsync();
            }
        }
    }
}