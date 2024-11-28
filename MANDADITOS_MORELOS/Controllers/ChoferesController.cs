using MANDADITOS_MORELOS.Functions;
using MANDADITOS_MORELOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Stripe;
using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChoferesController : ControllerBase
    {
        private readonly MorelosContext _context;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static List<WebSocket> _activeWebSockets = new List<WebSocket>();

        public ChoferesController(MorelosContext context)
        {
            _context = context;
        }

        [HttpGet("ws")]
        public async Task GetChoferesWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

                lock (_activeWebSockets)
                {
                    _activeWebSockets.Add(webSocket);
                }

                try
                {
                    var choferesList = await GetChoferesList();
                    var initialData = System.Text.Json.JsonSerializer.Serialize(choferesList);
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
                    lock (_activeWebSockets)
                    {
                        _activeWebSockets.Remove(webSocket);
                    }
                }
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task<List<ChoferesDTO>> GetChoferesList()
        {
            return await (from p in _context.Personas
                          join c in _context.Choferes on p.PersonaID equals c.PersonaID
                          join u in _context.Unidades on c.PersonaID equals u.ChoferID into unidadJoin
                          from u in unidadJoin.DefaultIfEmpty()
                          join v in _context.Valoraciones on c.PersonaID equals v.ChoferID into valoracionesJoin
                          from v in valoracionesJoin.DefaultIfEmpty()
                          group new { p, c, u, v } by new { p.PersonaID, p.Nombre, p.Apellidos, p.CorreoElectronico, p.Foto, c.Disponibilidad, p.ExpoPushToken } into groupedData
                          select new ChoferesDTO
                          {
                              PersonaID = groupedData.Key.PersonaID,
                              Nombre = groupedData.Key.Nombre,
                              Apellidos = groupedData.Key.Apellidos,
                              CorreoElectronico = groupedData.Key.CorreoElectronico,
                              Foto = groupedData.Key.Foto,
                              Disponibilidad = ConvertirDisponibilidad(groupedData.Key.Disponibilidad.ToString()),
                              ExpoPushToken = groupedData.Key.ExpoPushToken,
                              Unidad = groupedData.Select(g => g.u.Unidad).FirstOrDefault(),
                              Placa = groupedData.Select(g => g.u.Placa).FirstOrDefault(),
                              Marca = groupedData.Select(g => g.u.Marca).FirstOrDefault(),
                              Color = groupedData.Select(g => g.u.Color).FirstOrDefault(),
                              CantidadValoraciones = groupedData.Max(g => g.v.Cantidad),
                              Puntuacion = groupedData.Max(g => g.v.Puntuacion)
                          }).ToListAsync();
        }



        // GET: api/Choferes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChoferesDTO>>> GetChoferes()
        {
            var choferesList = await GetChoferesList();
            return Ok(choferesList);
        }

        private static int ConvertirDisponibilidad(string disponibilidad)
        {
            return disponibilidad switch
            {
                "Disponible" => 1,
                "Ocupado" => 2,
                _ => 3,
            };
        }

        // GET: api/Choferes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonasModel>> GetChoferesModel(int id)
        {
            var choferesModel = await (from p in _context.Personas
                                       join c in _context.Choferes
                                       on p.PersonaID equals c.PersonaID
                                       where p.PersonaID == id
                                       select p).FirstOrDefaultAsync();

            if (choferesModel == null)
            {
                return NotFound();
            }

            return choferesModel;
        }

        // PUT: api/Choferes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClientesModel(int id, ChoferesDTO choferesModel)
        {
            if (!ChoferesModelExists(id))
            {
                return NotFound();
            }

            var currentChofer = await _context.Choferes.FindAsync(id);
            var currentPerson = await _context.Personas.FindAsync(id);
            var currentUnit = await _context.Unidades.FirstOrDefaultAsync(u => u.ChoferID == id);
            var currentOrder = await _context.Pedidos.FirstOrDefaultAsync(p => p.ID == choferesModel.PedidoID);
            var currentRating = await _context.Valoraciones.FirstOrDefaultAsync(v => v.ChoferID == id);

            if (choferesModel.Puntuacion != null)
            {
                if (currentRating == null || currentOrder == null) return NotFound();

                currentOrder.Puntuado = true;

                if (choferesModel.Puntuacion == 0)
                {
                    _context.Entry(currentOrder).Property(p => p.Puntuado).IsModified = true;
                    await _context.SaveChangesAsync();
                    return NoContent();
                }

                
                currentRating.ChoferID = id;
                currentRating.Cantidad = -1;
                currentRating.Puntuacion = (float)choferesModel.Puntuacion;

                _context.Entry(currentOrder).Property(p => p.Puntuado).IsModified = true;
                _context.Entry(currentRating).Property(p => p.ChoferID).IsModified = true;
                _context.Entry(currentRating).Property(p => p.Cantidad).IsModified = true;
                _context.Entry(currentRating).Property(p => p.Puntuacion).IsModified = true;
                await _context.SaveChangesAsync();

                await NotifyClientsAboutUpdateAvailability();

                return NoContent();
            }

            if (choferesModel.ExpoPushToken != null)
            {
                currentPerson.ExpoPushToken = choferesModel.ExpoPushToken;
                _context.Entry(currentPerson).Property(p => p.ExpoPushToken).IsModified = true;
                await _context.SaveChangesAsync();

                await NotifyClientsAboutUpdateAvailability();
                return NoContent();
            }

            if (currentChofer == null || currentPerson == null || currentUnit == null)
            {
                return NotFound();
            }

            bool cardChanged =
                currentPerson.Nombre != choferesModel.Nombre || currentPerson.Apellidos != choferesModel.Apellidos
                || currentUnit.Unidad != choferesModel.Unidad
                || ConvertirDisponibilidad(currentChofer.Disponibilidad.ToString()) != choferesModel.Disponibilidad;

            try
            {
                await _context.Database.ExecuteSqlRawAsync("CALL sp_actualizar_chofer (@v_personaID, @v_nombre, @v_apellidos, @v_correoElectronico, @v_contrasenia, @v_foto, @v_disponibilidad)",
                    new MySqlParameter("@v_personaID", id),
                    new MySqlParameter("@v_nombre", choferesModel.Nombre ?? currentPerson.Nombre),
                    new MySqlParameter("@v_apellidos", choferesModel.Apellidos ?? currentPerson.Apellidos),
                    new MySqlParameter("@v_correoElectronico", choferesModel.CorreoElectronico ?? currentPerson.CorreoElectronico),
                    new MySqlParameter("@v_contrasenia", choferesModel.Contrasenia ?? currentPerson.Contrasenia),
                    new MySqlParameter("@v_foto", choferesModel.Foto ?? currentPerson.Foto),
                    new MySqlParameter("@v_disponibilidad", choferesModel.Disponibilidad ?? ConvertirDisponibilidad(currentChofer.Disponibilidad.ToString())
                    ));

                if (cardChanged)
                {
                    await NotifyClientsAboutUpdateAvailability();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error en la actualización: " + ex.Message);
            }
        }

        private async Task NotifyClientsAboutUpdateAvailability()
        {
            var choferesList = await GetChoferesList();

            var updatedData = System.Text.Json.JsonSerializer.Serialize(choferesList);
            var updatedDataBytes = Encoding.UTF8.GetBytes(updatedData);

            await _semaphore.WaitAsync();
            try
            {
                foreach (var webSocket in _activeWebSockets)
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(updatedDataBytes, 0, updatedDataBytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }



        // POST: api/Choferes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PersonasModel>> PostChoferesModel([FromBody] ChoferesDTO choferesModel)
        {
            await _context.Database.ExecuteSqlRawAsync("CALL sp_insertar_chofer (@v_nombre, @v_apellidos, @v_correo, @v_contrasenia, @v_foto, @v_disponibilidad)",
                new MySqlParameter("@v_nombre", choferesModel.Nombre),
                new MySqlParameter("@v_apellidos", choferesModel.Apellidos),
                new MySqlParameter("@v_correo", choferesModel.CorreoElectronico),
                new MySqlParameter("@v_contrasenia", Encrypt.EncryptSHA256(choferesModel.Contrasenia)),
                new MySqlParameter("@v_foto", choferesModel.Foto),
                new MySqlParameter("@v_disponibilidad", choferesModel.Disponibilidad));

            var nuevoChofer = await _context.Personas
                .FirstOrDefaultAsync(u => u.CorreoElectronico == choferesModel.CorreoElectronico);

            if (nuevoChofer == null)
            {
                return BadRequest("No se pudo insertar el usuario.");
            }

            return CreatedAtAction(nameof(PostChoferesModel), new { id = nuevoChofer.PersonaID }, nuevoChofer);
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

        private bool ChoferesModelExists(int id)
        {
            return _context.Choferes.Any(e => e.PersonaID == id);
        }
    }
}
