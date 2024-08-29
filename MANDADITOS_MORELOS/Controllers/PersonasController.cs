using MANDADITOS_MORELOS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonasController : ControllerBase
    {
        private readonly MorelosContext _context;
        private readonly IWebHostEnvironment _environment;

        public PersonasController(MorelosContext context, IWebHostEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _context = context;
        }

        // PUT api/<ValuesController>/user@gmail.com
        [HttpPut("uploadPhoto/{email}")]
        public async Task<IActionResult> PutPersonasModel(string email, [FromForm] IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
            {
                return BadRequest("No se ha enviado ninguna imagen.");
            }

            if (!foto.ContentType.StartsWith("image/"))
            {
                return BadRequest("El archivo enviado no es una imagen.");
            }

            var personasModel = await _context.Personas.FirstOrDefaultAsync(p => p.CorreoElectronico == email);
            if (personasModel == null)
            {
                return NotFound("Persona no encontrada.");
            }

            try
            {
                var uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "data");
                Directory.CreateDirectory(uploadsFolderPath);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(foto.FileName);
                var filePath = Path.Combine(uploadsFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                personasModel.Foto = $"data/{fileName}";
                _context.Entry(personasModel).Property(p => p.Foto).IsModified = true;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Error al procesar la solicitud: {ex.Message}");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private bool PersonasModelExists(int id)
        {
            return _context.Personas.Any(e => e.PersonaID == id);
        }
    }
}
