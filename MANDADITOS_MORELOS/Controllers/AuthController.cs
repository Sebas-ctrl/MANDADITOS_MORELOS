using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MANDADITOS_MORELOS.Models;
using System.Text;
using MANDADITOS_MORELOS.Functions;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;

namespace MANDADITOS_MORELOS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MorelosContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(MorelosContext context, IOptions<JwtSettings> jwtSettings, JwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _jwtTokenService = jwtTokenService;
        }

        // GET: api/Auth/token
        [HttpGet]
        public async Task<ActionResult<string>> AuthUserToken()
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (authorizationHeader == null || !authorizationHeader.StartsWith("Bearer "))
            {
                return BadRequest("Missing or invalid Authorization header");
            }

            // Extraer el token JWT
            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            object user = ProcessToken(token);

            if (user == null)
            {
                return BadRequest("Invalid token");
            }

            return Ok(user);
        }

        // POST: api/Auth
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PersonasModel>> AuthUser([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || !ModelState.IsValid)
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (authorizationHeader == null || !authorizationHeader.StartsWith("Bearer "))
                {
                    return BadRequest("Missing or invalid Authorization header");
                }

                // Extraer el token JWT
                var userToken = authorizationHeader.Substring("Bearer ".Length).Trim();

                object user = ProcessToken(userToken);

                if (user == null)
                {
                    return BadRequest("Invalid token");
                }

                return BadRequest(new { message = "InvalidRequest" });
            }

            var persona = await _context.Personas.FirstOrDefaultAsync(p => p.CorreoElectronico == loginRequest.Email);

            if (persona == null)
            {
                return NotFound(new { message = "UserNotFound" });
            }

            string hashedPassword = Encrypt.EncryptSHA256(loginRequest.Password);

            if (hashedPassword != persona.Contrasenia)
            {
                return Unauthorized(new { message = "InvalidPassword" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, persona.CorreoElectronico),
                new Claim(ClaimTypes.Name, persona.Nombre),
                new Claim("LastName", persona.Apellidos)
            };

            if (!string.IsNullOrEmpty(persona.Foto))
            {
                claims.Add(new Claim("Photo", persona.Foto));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = tokenString
            });
        }

        // Función para manejar la conexión WebSocket
        [HttpGet("ws")]
        public async Task HandleWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var buffer = new byte[1024 * 4];
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                while (!result.CloseStatus.HasValue)
                {
                    // Generar y enviar nuevo token JWT
                    var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                    if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
                    {
                        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                        var persona = ProcessToken(token);

                        if (persona != null)
                        {
                            var newToken = GenerateJwtToken(persona as PersonasModel);
                            var tokenBytes = Encoding.UTF8.GetBytes(newToken);

                            // Enviar el nuevo token al cliente
                            await webSocket.SendAsync(new ArraySegment<byte>(tokenBytes, 0, tokenBytes.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                        }
                    }

                    // Recibir más mensajes si es necesario
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }

                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private string GenerateJwtToken(PersonasModel persona)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim("Photo", persona.Foto),
                new Claim(ClaimTypes.Email, persona.CorreoElectronico),
                new Claim(ClaimTypes.Name, persona.Nombre),
                new Claim("LastName", persona.Apellidos)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private object ProcessToken(string token)
        {
            var principal = _jwtTokenService.ValidateJwtToken(token);

            if (principal != null)
            {
                var photo = principal.FindFirst("Photo")?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal.FindFirst(ClaimTypes.Name)?.Value;
                var lastName = principal.FindFirst("LastName")?.Value;

                return new PersonasModel
                {
                    Foto = photo,
                    CorreoElectronico = email,
                    Nombre = name,
                    Apellidos = lastName
                };
            }
            else
            {
                return null;
            }
        }
    }
}
