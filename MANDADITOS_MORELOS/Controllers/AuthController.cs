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

        // GET: api/Auth
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
            if (loginRequest == null || !ModelState.IsValid) return BadRequest(new { message = "InvalidRequest" });

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

            // Llamada a la función GenerateJwtToken para generar el token
            var tokenString = GenerateJwtToken(persona);

            var refreshToken = GenerateRefreshToken();
            persona.RefreshToken = refreshToken;

            await _context.SaveChangesAsync();

            var cliente = await _context.Clientes.FirstOrDefaultAsync(p => p.PersonaID == persona.PersonaID);

            if (cliente == null)
            {
                return Ok(new
                {
                    DriverToken = tokenString,
                    RefreshToken = refreshToken
                });
            }

            return Ok(new
            {
                ClientToken = tokenString,
                RefreshToken = refreshToken
            });

        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // Obtener el token del cuerpo de la solicitud
                var refreshToken = request.RefreshToken;

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest("Token no proporcionado.");
                }

                // Validar el token y obtener la información del usuario
                var persona = await _context.Personas.FirstOrDefaultAsync(p => p.RefreshToken == refreshToken);

                if (persona == null)
                {
                    return Unauthorized("Invalid refresh token.");
                }

                // Generar un nuevo token
                var newToken = GenerateJwtToken(persona as PersonasModel);
                var newRefreshToken = GenerateRefreshToken();

                // Almacenar el token de refresco en la base de datos
                persona.RefreshToken = newRefreshToken;

                await _context.SaveChangesAsync();

                // Devolver el nuevo token al cliente
                return Ok(new
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken
                });
            }catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                return null;
            }
        }

        // Clase de solicitud para el refresh token
        public class RefreshTokenRequest
        {
            public string RefreshToken { get; set; }
        }

        [HttpGet("ws")]
        public async Task HandleWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    var buffer = new byte[1024 * 4];

                    // Obtener el token de los parámetros de consulta en la URL
                    var queryToken = HttpContext.Request.Query["token"].ToString();

                    if (string.IsNullOrEmpty(queryToken))
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "No token provided", CancellationToken.None);
                        return;
                    }

                    // Procesar el token
                    var persona = ProcessToken(queryToken);
                    if (persona == null)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Invalid token", CancellationToken.None);
                        return;
                    }

                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    while (!result.CloseStatus.HasValue)
                    {
                        // Generar y enviar nuevo token JWT
                        var newToken = GenerateJwtToken(persona as PersonasModel);
                        var tokenBytes = Encoding.UTF8.GetBytes(newToken);

                        // Enviar el nuevo token al cliente
                        await webSocket.SendAsync(new ArraySegment<byte>(tokenBytes, 0, tokenBytes.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);

                        // Recibir más mensajes si es necesario
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }

                    // Cerrar el WebSocket
                    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"WebSocketException: {ex.ToString()}");
                }
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, persona.CorreoElectronico),
                new Claim(ClaimTypes.Name, persona.Nombre),
                new Claim("LastName", persona.Apellidos),
                new Claim("PersonaID", persona.PersonaID.ToString()),
            };

            if (!string.IsNullOrEmpty(persona.Foto))
            {
                claims.Add(new Claim("Photo", persona.Foto));
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }

        private object ProcessToken(string token)
        {
            var principal = _jwtTokenService.ValidateJwtToken(token);

            if (principal != null)
            {
                var photo = principal.FindFirst("Photo")?.Value;
                var personaID = principal.FindFirst("PersonaID")?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal.FindFirst(ClaimTypes.Name)?.Value;
                var lastName = principal.FindFirst("LastName")?.Value;

                return new PersonasModel
                {
                    Foto = photo,
                    PersonaID = int.Parse(personaID),
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
