using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosContext _context;

        public UsuariosController(UsuariosContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuariosModel>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuariosModel>> GetUsuariosModel(int id)
        {
            var usuariosModel = await _context.Usuarios.FindAsync(id);

            if (usuariosModel == null)
            {
                return NotFound();
            }

            return usuariosModel;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuariosModel(int id, UsuariosModel usuariosModel)
        {
            if (id != usuariosModel.ID)
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
        public async Task<ActionResult<UsuariosModel>> PostUsuariosModel(UsuariosModel usuariosModel)
        {
            _context.Usuarios.Add(usuariosModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(PostUsuariosModel), new { id = usuariosModel.ID }, usuariosModel);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuariosModel(int id)
        {
            var usuariosModel = await _context.Usuarios.FindAsync(id);
            if (usuariosModel == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuariosModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuariosModelExists(int id)
        {
            return _context.Usuarios.Any(e => e.ID == id);
        }
    }
}
