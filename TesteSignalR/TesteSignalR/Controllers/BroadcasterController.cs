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

namespace TesteSignalR.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BroadcasterController : ControllerBase
    {
        private static List<LogMessage> _itens = new List<LogMessage>();
        private static ConcurrentBag<StreamWriter> _clients = new ConcurrentBag<StreamWriter>();
        private readonly IHubContext<LogMessageHub> _streaming;

        public BroadcasterController(IHubContext<LogMessageHub> streaming) => _streaming = streaming;

        [HttpGet]
        public ActionResult<List<LogMessage>> Get() => _itens;

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
        public async Task<ActionResult<LogMessage>> Post([FromBody] LogMessage value)
        {
            if(value == null)
                return BadRequest();

            value.Id = Guid.NewGuid();
            value.Action = "Message added";

            _itens.Add(value);

            await WriteOnStream(value);

            return value;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<LogMessage>> Put(Guid id, [FromBody] LogMessage value)
        {
            var item = _itens.SingleOrDefault(i => i.Id == id);
            if(item != null)
            {
                _itens.Remove(item);
                value.Id = id;
                value.Action = "Item updated";
                _itens.Add(value);

                await WriteOnStream(value);

                return item;
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var item = _itens.SingleOrDefault(i => i.Id == id);
            if(item != null)
            {
                _itens.Remove(item);
                item.Action = "Message removed";
                await WriteOnStream(item);
                return Ok(new { Description = "Message removed" });
            }

            return BadRequest();
        }

        // Métodos privados
        private async Task WriteOnStream(LogMessage data)
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