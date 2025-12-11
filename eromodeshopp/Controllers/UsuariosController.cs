using eromodeshopp.Data;
using eromodeshopp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BC = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Authorization; // 👈 Importante para [Authorize]

namespace eromodeshopp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly EromodeshopDbContext _context;
        private readonly IConfiguration _configuration;

        public UsuariosController(EromodeshopDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<Usuario>> Register([FromBody] RegisterModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest("Email y contraseña son obligatorios.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest("El email ya está registrado.");
            }

            var passwordHash = BC.HashPassword(model.Password);

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                PasswordHash = passwordHash,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, new { Message = "Usuario creado con éxito." });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginModel model)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (usuario == null || !BC.Verify(model.Password, usuario.PasswordHash))
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var jwt = _configuration.GetSection("Jwt");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var role = usuario.EsAdmin ? "Admin" : "User";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim("role", role)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireInMinutes"])),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new AuthResponse
            {
                Token = tokenString,
                Email = usuario.Email,
                Nombre = usuario.Nombre
            });
        }

        // ⭐ CAMBIO CLAVE: Se agregó [Authorize]
        [HttpGet("perfil")]
        [Authorize]
        public async Task<ActionResult<PerfilDTO>> GetPerfilUsuario()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
            {
                // Este retorno 401 debería ser ahora manejado por el middleware de autenticación 
                // si el token es inválido o falta, pero se mantiene como fallback.
                return Unauthorized();
            }

            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
            {
                return NotFound();
            }

            // Mapea a PerfilDTO para no exponer el PasswordHash
            var perfilDto = new PerfilDTO
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                FechaCreacion = usuario.FechaCreacion
            };

            return Ok(perfilDto);
        }

        // NUEVO ENDPOINT: Obtener todos los usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            try
            {
                var usuarios = await _context.Usuarios.ToListAsync();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                // Log the exception (optional but recommended)
                return StatusCode(500, new { error = "Ocurrió un error interno al obtener los usuarios.", details = ex.Message });
            }
        }


        [HttpGet("generar-hash")]
        public IActionResult GenerarHashTemporal([FromQuery] string password = "1234")
        {
            var hash = BC.HashPassword(password);
            return Ok(new { password, hash });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return Ok(usuario);
        }
    }

    // ----------------------------------------------------
    // CLASES DTO Y MODELOS
    // ----------------------------------------------------

    public class RegisterModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Apellido { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    // Data Transfer Object para el Perfil (Seguro)
    public class PerfilDTO
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Apellido { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}