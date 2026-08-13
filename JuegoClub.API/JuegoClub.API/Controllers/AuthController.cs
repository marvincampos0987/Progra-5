using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JuegoClub.Dominio.DTO;
using JuegoClub.Dominio.Entidades;
using JuegoClub.LogicaNegocios.Implementaciones;
using JuegoClub.AccesoDatos.Contexto;

namespace JuegoClub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly JuegoClubContext _context;

        public AuthController(AuthService authService, JuegoClubContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registrar(RegistroDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("El correo y la contraseña son requeridos.");
            }

            bool emailExiste = await _context.Usuarios.AnyAsync(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            if (emailExiste)
            {
                return BadRequest("El correo ya se encuentra registrado.");
            }

            _authService.CrearPasswordHash(dto.Password, out byte[] hash, out byte[] salt);

            var nuevoUsuario = new Usuario
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Monedas = 1000,
                Nivel = 1
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Jugador registrado con éxito. Progreso listo para guardarse automáticamente." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("El correo y la contraseña son requeridos.");
            }

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase));
            if (usuario == null)
            {
                return Unauthorized("Correo no registrado.");
            }

            if (!_authService.VerificarPassword(dto.Password, usuario.PasswordHash, usuario.PasswordSalt))
            {
                return Unauthorized("Contraseña incorrecta.");
            }

            // Generar token JWT simulado
            return Ok(new { Token = "jwt_token_simulado", Username = usuario.Username });
        }
    }
}