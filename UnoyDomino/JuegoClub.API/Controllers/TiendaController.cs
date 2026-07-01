using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JuegoClub.AccesoDatos.Contexto;
using JuegoClub.Dominio.Entidades;

namespace JuegoClub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TiendaController : ControllerBase
    {
        private readonly JuegoClubContext _context;

        public TiendaController(JuegoClubContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el catálogo de artículos disponibles en la tienda.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerArticulos()
        {
            var articulos = await _context.Articulos.ToListAsync();
            return Ok(articulos);
        }

        /// <summary>
        /// Obtiene los artículos comprados e inventario de un usuario.
        /// </summary>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerInventario(int usuarioId)
        {
            var inventario = await _context.UsuarioArticulos
                .Include(ua => ua.Articulo)
                .Where(ua => ua.UsuarioId == usuarioId)
                .Select(ua => new
                {
                    ArticuloId = ua.ArticuloId,
                    Nombre = ua.Articulo != null ? ua.Articulo.Nombre : "Cosmético",
                    Descripcion = ua.Articulo != null ? ua.Articulo.Descripcion : "",
                    Tipo = ua.Articulo != null ? ua.Articulo.Tipo.ToString() : "",
                    Valor = ua.Articulo != null ? ua.Articulo.Valor : "",
                    FechaCompra = ua.FechaCompra,
                    Equipado = ua.Equipado
                })
                .ToListAsync();

            return Ok(inventario);
        }

        /// <summary>
        /// Compra un artículo de personalización.
        /// </summary>
        [HttpPost("comprar/{usuarioId}/{articuloId}")]
        public async Task<IActionResult> ComprarArticulo(int usuarioId, int articuloId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound("Usuario no encontrado.");

            var articulo = await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId);
            if (articulo == null) return NotFound("Artículo no encontrado.");

            bool yaComprado = await _context.UsuarioArticulos.AnyAsync(ua => ua.UsuarioId == usuarioId && ua.ArticuloId == articuloId);
            if (yaComprado) return BadRequest("Ya has comprado este artículo.");

            if (usuario.Monedas < articulo.Precio)
            {
                return BadRequest("No tienes suficientes monedas.");
            }

            usuario.Monedas -= articulo.Precio;

            var compra = new UsuarioArticulo
            {
                UsuarioId = usuarioId,
                ArticuloId = articuloId,
                FechaCompra = DateTime.UtcNow,
                Equipado = false
            };

            _context.UsuarioArticulos.Add(compra);
            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = $"Compraste {articulo.Nombre} con éxito.", MonedasRestantes = usuario.Monedas });
        }

        /// <summary>
        /// Equipa un artículo cosmético comprado.
        /// </summary>
        [HttpPost("equipar/{usuarioId}/{articuloId}")]
        public async Task<IActionResult> EquiparArticulo(int usuarioId, int articuloId)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null) return NotFound("Usuario no encontrado.");

            var articulo = await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId);
            if (articulo == null) return NotFound("Artículo no encontrado.");

            var usuarioArticulo = await _context.UsuarioArticulos
                .FirstOrDefaultAsync(ua => ua.UsuarioId == usuarioId && ua.ArticuloId == articuloId);

            if (usuarioArticulo == null)
            {
                return BadRequest("No eres dueño de este artículo.");
            }

            // Desequipar otros artículos del mismo tipo
            var otrosDelMismoTipo = await _context.UsuarioArticulos
                .Include(ua => ua.Articulo)
                .Where(ua => ua.UsuarioId == usuarioId && ua.Articulo != null && ua.Articulo.Tipo == articulo.Tipo && ua.ArticuloId != articuloId)
                .ToListAsync();

            foreach (var item in otrosDelMismoTipo)
            {
                item.Equipado = false;
            }

            usuarioArticulo.Equipado = true;

            // Aplicar cambio al perfil de usuario
            if (articulo.Tipo == TipoArticulo.BordeAvatar)
            {
                usuario.AvatarBorde = articulo.Valor;
            }
            else if (articulo.Tipo == TipoArticulo.AspectoTablero)
            {
                usuario.AspectoJuego = articulo.Valor;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Mensaje = $"Equipaste {articulo.Nombre} con éxito." });
        }
    }
}
