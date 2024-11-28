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
using static MANDADITOS_MORELOS.Controllers.PedidosController;

namespace MANDADITOS_MORELOS.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly MorelosContext _context;

        public PedidosController(MorelosContext context)
        {
            _context = context;
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

        private async Task<List<PedidosDTO>> GetPedidosListForId(int id)
        {
            return await (from p in _context.Pedidos
                          where p.ID == id
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

        private async Task<PaginacionDTO<PedidosDTO>> GetPedidosListForUser(int usuarioId, int pagina)
        {
            int pageSize = 20;
            int skip = (pagina - 1) * pageSize;

            var totalRegistros = await (from p in _context.Pedidos
                                        where p.ClienteID == usuarioId || p.ChoferID == usuarioId
                                        select p).CountAsync();

            int totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var pedidos = await (from p in _context.Pedidos
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
                          orderby p.FechaInicio descending
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
                          })
                          .Skip(skip)
                          .Take(pageSize)
                          .ToListAsync();

            return new PaginacionDTO<PedidosDTO>
            {
                Datos = pedidos,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                RegistrosPorPagina = pageSize,
                TotalRegistros = totalRegistros
            };
        }

        public class PaginacionDTO<T>
        {
            public List<T> Datos { get; set; }
            public int PaginaActual { get; set; }
            public int TotalPaginas { get; set; }
            public int RegistrosPorPagina { get; set; }
            public int TotalRegistros { get; set; }
        }


        private async Task<List<PedidosDTO>> GetPedidosPendientesListForUser(int usuarioId)
        {
            return await (from p in _context.Pedidos
                          where (p.ClienteID == usuarioId || p.ChoferID == usuarioId)
                            && (p.Estatus == TipoEstado.Pendiente || p.Estatus == TipoEstado.EnCurso)
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

        private async Task<List<PedidosDTO>> GetPedidosNoPuntuadosListForUser(int usuarioId)
        {
            return await (from p in _context.Pedidos
                          where (p.ClienteID == usuarioId || p.ChoferID == usuarioId)
                            && p.Estatus == TipoEstado.Completado && (p.Puntuado == false || p.Puntuado == null)
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
        [HttpGet("porUsuario/{usuarioId}")]
        public async Task<ActionResult<PedidosDTO>> GetPedidosPorUsuarioModel(int usuarioId, int pagina)
        {
            var pedidos = await GetPedidosListForUser(usuarioId, pagina);
            if (pedidos == null)
            {
                return NotFound();
            }

            return Ok(pedidos);
        }

        [HttpGet("porId/{id}")]
        public async Task<ActionResult<PedidosDTO>> GetPedidoPorIdModel(int id)
        {
            var pedido = await GetPedidosListForId(id);

            if (pedido == null)
            {
                return NotFound();
            }

            return Ok(pedido);
        }

        [HttpGet("pedidosPendientes/{usuarioId}")]
        public async Task<ActionResult<PedidosDTO>> GetPedidosPendientesModel(int usuarioId)
        {
            var pedidos = await GetPedidosPendientesListForUser(usuarioId);
            if (pedidos == null)
            {
                return NotFound();
            }

            return Ok(pedidos);
        }

        [HttpGet("pedidosNoPuntuados/{usuarioId}")]
        public async Task<ActionResult<PedidosDTO>> GetPedidosNoPuntuadosModel(int usuarioId)
        {
            var pedidos = await GetPedidosNoPuntuadosListForUser(usuarioId);
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

                return Ok(new { message = "successful", pedido.ID });
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        // DELETE: api/Choferes/5
        [HttpDelete("pedidoCancelado/{id}")]
        public async Task<IActionResult> DeletePedidoCanceladoModel(int id)
        {
            var pedidosModel = await _context.Pedidos.FindAsync(id);
            if (pedidosModel != null)
            {
                _context.Pedidos.Remove(pedidosModel);
                await _context.SaveChangesAsync();

                var pagosModel = await _context.Pagos.FindAsync(pedidosModel.PagoID);
                if (pagosModel != null)
                {
                    _context.Pagos.Remove(pagosModel);
                    await _context.SaveChangesAsync();
                }
            }

            return NoContent();
        }

        // DELETE: api/Choferes/5
        [HttpDelete("pedidoAgotado/{id}")]
        public async Task<IActionResult> DeletePedidoAgotadoModel(int id)
        {
            var pedidosModel = await _context.Pedidos.FindAsync(id);
            if (pedidosModel == null)
            {
                return NotFound();
            }

            _context.Pedidos.Remove(pedidosModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PedidosModelExists(int id)
        {
            return _context.Pedidos.Any(p => p.ID == id);
        }
    }
}
