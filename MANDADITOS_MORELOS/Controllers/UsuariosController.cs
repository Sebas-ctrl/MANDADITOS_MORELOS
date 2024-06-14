using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using System.Data.SqlClient;
using MySqlConnector;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly MorelosContext _context;

        public UsuariosController(MorelosContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonasModel>>> GetUsuarios()
        {
            return await _context.Personas.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PersonasModel>> GetUsuariosModel(int id)
        {
            var usuariosModel = await _context.Personas.FindAsync(id);

            if (usuariosModel == null)
            {
                return NotFound();
            }

            return usuariosModel;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuariosModel(int id, PersonasModel usuariosModel)
        {
            if (id != usuariosModel.PersonaID)
            {
                return BadRequest();
            }

            _context.Entry(usuariosModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuariosModelExists(id))
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

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PersonasModel>> PostUsuariosModel(PersonasModel personasModel)
        {
            await _context.Database.ExecuteSqlRawAsync("CALL sp_insertar_usuario (@v_nombre, @v_correo, @v_contrasenia)",
                new MySqlParameter("@v_nombre", personasModel.Nombre),
                new MySqlParameter("@v_correo", personasModel.CorreoElectronico),
                new MySqlParameter("@v_contrasenia", personasModel.Contrasenia));

            var nuevoUsuario = await _context.Personas
                .FirstOrDefaultAsync(u => u.CorreoElectronico== personasModel.CorreoElectronico);

            if (nuevoUsuario == null)
            {
                return BadRequest("No se pudo insertar el usuario.");
            }

            return CreatedAtAction(nameof(PostUsuariosModel), new { id = nuevoUsuario.PersonaID }, nuevoUsuario);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuariosModel(int id)
        {
            var usuariosModel = await _context.Personas.FindAsync(id);
            if (usuariosModel == null)
            {
                return NotFound();
            }

            _context.Personas.Remove(usuariosModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuariosModelExists(int id)
        {
            return _context.Personas.Any(e => e.PersonaID == id);
        }
    }
}
