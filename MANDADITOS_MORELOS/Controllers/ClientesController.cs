using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using MySqlConnector;
using MANDADITOS_MORELOS.Functions;
using System;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly MorelosContext _context;

        public ClientesController(MorelosContext context)
        {
            _context = context;
        }

        // GET: api/Clientes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonasModel>>> GetClientes()
        {
            var clientesModel = await (from p in _context.Personas
                                       join c in _context.Clientes
                                       on p.PersonaID equals c.PersonaID
                                       select p).ToListAsync();

            return clientesModel;
        }

        // GET: api/Clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonasModel>> GetClientesModel(int id)
        {
            var clientesModel = await (from p in _context.Personas
                                       join c in _context.Clientes
                                       on p.PersonaID equals c.PersonaID
                                       where p.PersonaID == id
                                       select p).FirstOrDefaultAsync();

            if (clientesModel == null)
            {
                return NotFound();
            }

            return clientesModel;
        }

        // PUT: api/Clientes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutClientesModel(int id, PersonasModel personasModel)
        {
            if (id != personasModel.PersonaID)
            {
                return BadRequest();
            }

            _context.Entry(personasModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientesModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Clientes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PersonasModel>> PostClientesModel([FromBody] PersonasModel personasModel)
        {
            await _context.Database.ExecuteSqlRawAsync("CALL sp_insertar_cliente (@v_nombre, @v_apellidos, @v_correo, @v_contrasenia, @v_foto)",
                new MySqlParameter("@v_nombre", personasModel.Nombre),
                new MySqlParameter("@v_apellidos", personasModel.Apellidos),
                new MySqlParameter("@v_correo", personasModel.CorreoElectronico),
                new MySqlParameter("@v_contrasenia", Encrypt.EncryptSHA256(personasModel.Contrasenia)),
                new MySqlParameter("@v_foto", personasModel.Foto));

            var nuevoCliente = await _context.Personas
                .FirstOrDefaultAsync(u => u.CorreoElectronico== personasModel.CorreoElectronico);

            if (nuevoCliente == null)
            {
                return BadRequest("No se pudo insertar el usuario.");
            }

            return CreatedAtAction(nameof(PostClientesModel), new { id = nuevoCliente.PersonaID }, nuevoCliente);
        }

        // DELETE: api/Clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClientesModel(int id)
        {
            var clientesModel = await _context.Personas.FindAsync(id);
            if (clientesModel == null)
            {
                return NotFound();
            }

            _context.Personas.Remove(clientesModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClientesModelExists(int id)
        {
            return _context.Clientes.Any(e => e.PersonaID == id);
        }
    }
}
