using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonasController : ControllerBase
    {
        private readonly MorelosContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName = "mandaditos-morelos";

        public PersonasController(MorelosContext context, IAmazonS3 s3Client)
        {
            _context = context;
            _s3Client = s3Client;
        }

        // PUT: api/Personas/5
        [HttpPut("{email}")]
        public async Task<IActionResult> PutPersonasModel(string email, PersonasModel personasModel)
        {
            var person = await _context.Personas.FirstOrDefaultAsync(p => p.CorreoElectronico == email);
            if (person == null) return NotFound("Persona no encontrada.");
            try
            {
                person.Nombre = personasModel.Nombre ?? person.Nombre;
                person.Apellidos = personasModel.Apellidos ?? person.Apellidos;
                person.ExpoPushToken = personasModel.ExpoPushToken;

                _context.Entry(person).Property(p => p.Nombre).IsModified = true;
                _context.Entry(person).Property(p => p.Apellidos).IsModified = true;
                _context.Entry(person).Property(p => p.ExpoPushToken).IsModified = true;

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

        // PUT api/personas/uploadPhoto/user@gmail.com
        [HttpPut("uploadPhoto/{email}")]
        public async Task<IActionResult> PutPersonasPhoto(string email, [FromForm] IFormFile foto)
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
                if (!string.IsNullOrEmpty(personasModel.Foto))
                {
                    var existingFileKey = personasModel.Foto.Replace($"https://{_bucketName}.s3.amazonaws.com/", "");
                    if (!string.IsNullOrEmpty(existingFileKey))
                    {
                        var deleteRequest = new DeleteObjectRequest
                        {
                            BucketName = _bucketName,
                            Key = existingFileKey
                        };
                        await _s3Client.DeleteObjectAsync(deleteRequest);
                    }
                }

                // Generar un nombre único para el archivo
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(foto.FileName);
                var key = $"images/{fileName}";

                // Subir el archivo a S3
                using (var stream = foto.OpenReadStream())
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = stream,
                        ContentType = foto.ContentType
                    };
                    await _s3Client.PutObjectAsync(putRequest);
                }

                // Guardar la URL pública en la base de datos
                var fileUrl = $"https://{_bucketName}.s3.amazonaws.com/{key}";
                personasModel.Foto = fileUrl;
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

        [HttpGet("getPhoto/{email}")]
        public async Task<IActionResult> GetPhoto(string email)
        {
            var personasModel = await _context.Personas.FirstOrDefaultAsync(p => p.CorreoElectronico == email);
            if (personasModel == null || string.IsNullOrEmpty(personasModel.Foto))
            {
                return NotFound("Persona o foto no encontrada.");
            }

            // Devolver la URL de la imagen almacenada en la base de datos
            return Ok(personasModel.Foto);
        }
    }
}
