using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using MySqlConnector;
using MANDADITOS_MORELOS.Functions;
using System;
using Stripe;

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
            var persona = await (from p in _context.Personas
                                       join c in _context.Clientes
                                       on p.PersonaID equals c.PersonaID
                                       where p.PersonaID == id
                                       select p).FirstOrDefaultAsync();

            if (persona == null)
            {
                return NotFound();
            }

            if (personasModel.Nombre != null)
            {
                persona.Nombre = personasModel.Nombre;
            }

            if (personasModel.Apellidos != null)
            {
                persona.Apellidos = personasModel.Apellidos;
            }

            if (personasModel.CorreoElectronico != null)
            {
                persona.CorreoElectronico = personasModel.CorreoElectronico;
            }


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
        public async Task<ActionResult<PersonasModel>> PostClientesModel([FromBody] InsertCliente insertCliente)
        {
            await _context.Database.ExecuteSqlRawAsync("CALL sp_insertar_cliente (@v_nombre, @v_apellidos, @v_correo, @v_contrasenia, @v_foto, @v_metodo_login)",
                new MySqlParameter("@v_nombre", insertCliente.persona.Nombre),
                new MySqlParameter("@v_apellidos", insertCliente.persona.Apellidos),
                new MySqlParameter("@v_correo", insertCliente.persona.CorreoElectronico),
                new MySqlParameter("@v_contrasenia", insertCliente.persona.Contrasenia != null ? Encrypt.EncryptSHA256(insertCliente.persona.Contrasenia) : null),
                new MySqlParameter("@v_foto", insertCliente.persona.Foto),
                new MySqlParameter("@v_metodo_login", insertCliente.MetodoLogin));

            var nuevoCliente = await _context.Personas
                .FirstOrDefaultAsync(u => u.CorreoElectronico== insertCliente.persona.CorreoElectronico);

            if (nuevoCliente == null)
            {
                return BadRequest("No se pudo insertar el usuario.");
            }

            return CreatedAtAction(nameof(PostClientesModel), new { id = nuevoCliente.PersonaID }, nuevoCliente);
        }

        public class InsertCliente
        {
            public PersonasModel? persona { get; set; }
            public Metodo? MetodoLogin { get; set; }

            public enum Metodo
            {
                MandaditosMorelos = 0,
                Google = 1,
                Facebook = 2
            }
        }

        // DELETE: api/Clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClientesModel(int id)
        {
            var personasModel = await _context.Personas.FindAsync(id);
            var clientesModel = await _context.Clientes.FindAsync(id);
            var googleAccount = await _context.GoogleAccounts.FindAsync(id);
            if (personasModel == null || clientesModel == null)
            {
                return NotFound();
            }

            if(googleAccount != null)
            {
                _context.GoogleAccounts.Remove(googleAccount);
                await _context.SaveChangesAsync();
            }

            _context.Clientes.Remove(clientesModel);
            await _context.SaveChangesAsync();
            _context.Personas.Remove(personasModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClientesModelExists(int id)
        {
            return _context.Clientes.Any(e => e.PersonaID == id);
        }
    }
}
