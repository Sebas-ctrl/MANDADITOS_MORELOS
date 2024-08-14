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
        [HttpGet("{token}")]
        public async Task<ActionResult<string>> AuthUserToken(string token)
        {
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
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim(ClaimTypes.Email, persona.CorreoElectronico),
                new Claim(ClaimTypes.Name, persona.Nombre),
                new Claim("LastName", persona.Apellidos)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var refreshToken = GenerateRefreshToken();

            persona.RefreshToken = refreshToken;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = tokenString,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenReq request)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Invalid token or refresh token" });
            }

            ClaimsPrincipal principal;

            try
            {
                principal = GetPrincipalFromExpiredToken(request.AccessToken);
            }
            catch (SecurityTokenException)
            {
                return Unauthorized(new { message = "Invalid or expired access token" });
            }
            catch (Exception)
            {
                return Unauthorized(new { message = "Token validation failed" });
            }

            var email = principal.Identity.Name;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var persona = await _context.Personas.FirstOrDefaultAsync(p => p.CorreoElectronico == email);

            if (persona == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            // Log the tokens for debugging
            Console.WriteLine($"Stored Refresh Token: {persona.RefreshToken}");
            Console.WriteLine($"Requested Refresh Token: {request.RefreshToken}");
            Console.WriteLine($"Email from token: {email}");

            if (request.RefreshToken != persona.RefreshToken)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var newToken = GenerateJwtToken(persona);
            var newRefreshToken = GenerateRefreshToken();

            // Update the refresh token in the database
            persona.RefreshToken = newRefreshToken;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = newToken,
                RefreshToken = newRefreshToken
            });
        }


        private string GenerateJwtToken(PersonasModel persona)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
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
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = principal.FindFirst(ClaimTypes.Name)?.Value;
                var lastName = principal.FindFirst("LastName")?.Value;

                return Ok(new
                {
                    Name = name,
                    LastName = lastName,
                    Email = email
                });
            }
            else
            {
                return "Token inválido.";
            }
        }

        private string GenerateRefreshToken()
        {
            // Implementar lógica para generar un token de actualización
            return Guid.NewGuid().ToString(); // Ejemplo simple, considera usar una solución más segura
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.SecretKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false // Permite validar un token expirado
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // Verifica si el token es un JwtSecurityToken
                if (validatedToken is not JwtSecurityToken jwtToken)
                {
                    throw new SecurityTokenException("Invalid token type");
                }

                return principal;
            }
            catch (SecurityTokenException ex)
            {
                // Manejo del error y registro
                throw new SecurityTokenException("Token validation failed", ex);
            }
            catch (Exception ex)
            {
                // Manejo del error general y registro
                throw new SecurityTokenException("Token validation failed", ex);
            }
        }


    }
}
