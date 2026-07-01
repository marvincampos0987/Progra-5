using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JuegoClub.Dominio.DTO;
using JuegoClub.Dominio.Entidades;
using JuegoClub.AccesoDatos.Contexto;

namespace JuegoClub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerfilController : ControllerBase
    {
        private readonly JuegoClubContext _context;

        public PerfilController(JuegoClubContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PerfilUsuarioDto>> ObtenerPerfil(int id)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            var dto = new PerfilUsuarioDto
            {
                Id = usuario.Id,
                Username = usuario.Username,
                AvatarUrl = usuario.AvatarUrl,
                ColorPerfil = usuario.ColorPerfil,
                Monedas = usuario.Monedas,
                Nivel = usuario.Nivel,
                ExperienciaActual = usuario.ExperienciaActual,
                ExperienciaSiguienteNivel = usuario.ExperienciaSiguienteNivel
            };

            return Ok(dto);
        }

        [HttpPut("{id}/personalizar")]
        public async Task<IActionResult> PersonalizarPerfil(int id, [FromBody] PersonalizarPerfilDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            usuario.AvatarUrl = dto.AvatarUrl;
            usuario.ColorPerfil = dto.ColorPerfil;

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = "Perfil actualizado con éxito." });
        }
    }
}
