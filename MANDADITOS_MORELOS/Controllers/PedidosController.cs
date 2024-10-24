using MANDADITOS_MORELOS.Functions;
using MANDADITOS_MORELOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Stripe.Forwarding;
using Stripe;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System;

namespace MANDADITOS_MORELOS.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly MorelosContext _context;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static Dictionary<int, WebSocket> _userWebSockets = new Dictionary<int, WebSocket>();

        public PedidosController(MorelosContext context)
        {
            _context = context;
        }

        [HttpGet("ws/{usuarioId}")]
        public async Task GetPedidosWebSocket(int usuarioId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                lock (_userWebSockets)
                {
                    _userWebSockets[usuarioId] = webSocket;
                }

                try
                {
                    // Enviar datos iniciales al usuario.
                    var pedidosList = await GetPedidosListForUser(usuarioId);
                    var initialData = JsonSerializer.Serialize(pedidosList);
                    var initialDataBytes = Encoding.UTF8.GetBytes(initialData);
                    await webSocket.SendAsync(new ArraySegment<byte>(initialDataBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new byte[1024 * 4];
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    } while (!result.CloseStatus.HasValue);

                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
                finally
                {
                    lock (_userWebSockets)
                    {
                        _userWebSockets.Remove(usuarioId);
                    }
                }
            }
            else
            {
                Console.WriteLine("Petición Inválida: No es un WebSocket");
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task<List<PedidosDTO>> GetPedidosList()
        {
            return await (from p in _context.Pedidos
                          join peC in _context.Personas on p.ClienteID equals peC.PersonaID
                          join peD in _context.Personas on p.ChoferID equals peD.PersonaID into peDJoin
                          from peD in peDJoin.DefaultIfEmpty()
                          join d in _context.Choferes on p.ChoferID equals d.PersonaID into choferJoin
                          from d in choferJoin.DefaultIfEmpty()
                          join c in _context.Clientes on p.ClienteID equals c.PersonaID into clienteJoin
                          from c in clienteJoin.DefaultIfEmpty()
                          join pa in _context.Pagos on p.PagoID equals pa.ID into pagosJoin
                          from pa in pagosJoin.DefaultIfEmpty()
                          select new PedidosDTO
                          {
                              ID = p.ID,
                              Tipo = ConvertirTipoPedido(p.Tipo.ToString()),
                              LugarOrigen = p.LugarOrigen,
                              LugarDestino = p.LugarDestino,
                              FechaInicio = p.FechaInicio,
                              FechaFin = p.FechaFin,
                              Estatus = ConvertirEstatus(p.Estatus.ToString()),
                              Cliente = new PersonasModel
                              {
                                  PersonaID = peC.PersonaID,
                                  Nombre = peC.Nombre,
                                  Apellidos = peC.Apellidos,
                                  CorreoElectronico = peC.CorreoElectronico,
                                  Foto = peC.Foto,
                                  ExpoPushToken = peC.ExpoPushToken
                              },
                              Chofer = new PersonasModel
                              {
                                  PersonaID = peD.PersonaID,
                                  Nombre = peD.Nombre,
                                  Apellidos = peD.Apellidos,
                                  CorreoElectronico = peD.CorreoElectronico,
                                  Foto = peD.Foto,
                                  ExpoPushToken = peD.ExpoPushToken
                              },
                              Pago = new PagosModel
                              {
                                  ID = pa.ID,
                                  Estatus = pa.Estatus,
                                  Monto = pa.Monto
                              }
                          }).ToListAsync();

        }

        private async Task<List<PedidosDTO>> GetPedidosListForUser(int usuarioId)
        {
            return await (from p in _context.Pedidos
                          where p.ClienteID == usuarioId || p.ChoferID == usuarioId
                          join peC in _context.Personas on p.ClienteID equals peC.PersonaID
                          join peD in _context.Personas on p.ChoferID equals peD.PersonaID into peDJoin
                          from peD in peDJoin.DefaultIfEmpty()
                          join d in _context.Choferes on p.ChoferID equals d.PersonaID into choferJoin
                          from d in choferJoin.DefaultIfEmpty()
                          join c in _context.Clientes on p.ClienteID equals c.PersonaID into clienteJoin
                          from c in clienteJoin.DefaultIfEmpty()
                          join pa in _context.Pagos on p.PagoID equals pa.ID into pagosJoin
                          from pa in pagosJoin.DefaultIfEmpty()
                          select new PedidosDTO
                          {
                              ID = p.ID,
                              Tipo = ConvertirTipoPedido(p.Tipo.ToString()),
                              LugarOrigen = p.LugarOrigen,
                              LugarDestino = p.LugarDestino,
                              FechaInicio = p.FechaInicio,
                              FechaFin = p.FechaFin,
                              Estatus = ConvertirEstatus(p.Estatus.ToString()),
                              Cliente = new PersonasModel
                              {
                                  PersonaID = peC.PersonaID,
                                  Nombre = peC.Nombre,
                                  Apellidos = peC.Apellidos,
                                  CorreoElectronico = peC.CorreoElectronico,
                                  Foto = peC.Foto,
                                  ExpoPushToken = peC.ExpoPushToken
                              },
                              Chofer = new PersonasModel
                              {
                                  PersonaID = peD.PersonaID,
                                  Nombre = peD.Nombre,
                                  Apellidos = peD.Apellidos,
                                  CorreoElectronico = peD.CorreoElectronico,
                                  Foto = peD.Foto,
                                  ExpoPushToken = peD.ExpoPushToken
                              },
                              Pago = new PagosModel
                              {
                                  ID = pa.ID,
                                  Estatus = pa.Estatus,
                                  Monto = pa.Monto
                              }
                          }).ToListAsync();
        }


        // GET: api/Pedidos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PedidosDTO>>> GetPedidos()
        {
            var pedidosList = await GetPedidosList();
            return Ok(pedidosList);
        }

        private static int ConvertirTipoPedido(string tipoPedido)
        {
            return tipoPedido switch
            {
                "Envio" => 1,
                "Recibo" => 2,
                _ => 0,
            };
        }
        private static int ConvertirEstatus(string estatus)
        {
            return estatus switch
            {
                "Pendiente" => 1,
                "EnCurso" => 2,
                "Completado" => 3,
                "Cancelado" => 4,
                _ => 0,
            };
        }

        // GET: api/Pedidos/5
        [HttpGet("{usuarioId}")]
        public async Task<ActionResult<PedidosDTO>> GetPedidosModel(int usuarioId)
        {
            var pedidos = await GetPedidosListForUser(usuarioId);
            if (pedidos == null)
            {
                return NotFound();
            }

            return Ok(pedidos);
        }

        // PUT: api/Pedidos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPedidosModel(int id, PedidosUpdate pedidosUpdate)
        {
            var pedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.ID == id);
            if (!PedidosModelExists(id))
            {
                return NotFound();
            }

            try
            {
                pedido.FechaFin = pedidosUpdate.FechaFin.HasValue ? pedidosUpdate.FechaFin : pedido.FechaFin;
                TipoEstado estatus = new();

                switch (pedidosUpdate.Estatus)
                {
                    case 1: 
                        estatus = TipoEstado.Pendiente; break;
                    case 2:
                        estatus = TipoEstado.EnCurso; break;
                    case 3:
                        estatus = TipoEstado.Completado; break;
                    case 4:
                        estatus = TipoEstado.Cancelado; break;
                    default: break;
                }

                pedido.Estatus = pedidosUpdate.Estatus.HasValue ? estatus : pedido.Estatus;

                _context.Entry(pedido).Property(p => p.FechaFin).IsModified = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en la actualización: " + ex.Message);
            }
        }

        public class PedidosUpdate
        {
            public DateTime? FechaFin { get; set; }
            public int? Estatus { get; set; }
        }

        private async Task NotifyClientsAboutUpdateOrder(int usuarioId)
        {
            var pedidosList = await GetPedidosListForUser(usuarioId);
            var updatedData = JsonSerializer.Serialize(pedidosList);
            var updatedDataBytes = Encoding.UTF8.GetBytes(updatedData);

            // Usar el semáforo para bloquear y permitir operaciones async
            await _semaphore.WaitAsync();
            try
            {
                // Verificar si el WebSocket está abierto
                if (_userWebSockets.TryGetValue(usuarioId, out var webSocket) && webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(updatedDataBytes, 0, updatedDataBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            finally
            {
                _semaphore.Release(); // Liberar el semáforo al final
            }
        }



        // POST: api/Pedidos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PedidosModel>> PostPedidosModel([FromBody] PedidosInsert pedidosInsert)
        {
            try
            {
                var coordenadasOrigen = new { Latitud = pedidosInsert.LatitudOrigen, Longitud = pedidosInsert.LongitudOrigen };
                var coordenadasDestino = new { Latitud = pedidosInsert.LatitudDestino, Longitud = pedidosInsert.LongitudDestino };
                DateTime fechaActual = DateTime.Now;
                var pedido = new PedidosModel
                {
                    Tipo = pedidosInsert.Tipo,
                    LugarOrigen = JsonSerializer.Serialize(coordenadasOrigen),
                    LugarDestino = JsonSerializer.Serialize(coordenadasDestino),
                    FechaInicio = fechaActual,
                    Estatus = TipoEstado.Pendiente,
                    ClienteID = pedidosInsert.ClienteID,
                    ChoferID = pedidosInsert.ChoferID,
                    PagoID = pedidosInsert.PagoID
                };

                _context.Pedidos.Add(pedido);
                _context.SaveChanges();

                await NotifyClientsAboutUpdateOrder(pedidosInsert.ClienteID);
                await NotifyClientsAboutUpdateOrder(pedidosInsert.ChoferID);

                return Ok(new { message = "successful", pedido.ID });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        // DELETE: api/Choferes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChoferesModel(int id)
        {
            var choferesModel = await _context.Choferes.FindAsync(id);
            if (choferesModel == null)
            {
                return NotFound();
            }

            _context.Choferes.Remove(choferesModel);

            var personaModel = await _context.Personas.FindAsync(id);
            if (personaModel != null)
            {
                _context.Personas.Remove(personaModel);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PedidosModelExists(int id)
        {
            return _context.Pedidos.Any(p => p.ID == id);
        }
    }
}
