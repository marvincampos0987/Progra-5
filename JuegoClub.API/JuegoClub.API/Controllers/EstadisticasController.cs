using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JuegoClub.AccesoDatos.Contexto;
using JuegoClub.Dominio.Entidades;

namespace JuegoClub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadisticasController : ControllerBase
    {
        private readonly JuegoClubContext _context;

        public EstadisticasController(JuegoClubContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Requerimiento 7: Historial de partidas recientes de un jugador.
        /// </summary>
        [HttpGet("historial/{usuarioId}")]
        public async Task<IActionResult> ObtenerHistorial(int usuarioId)
        {
            var historial = await _context.HistorialPartidas
                .Where(h => h.UsuarioId == usuarioId)
                .OrderByDescending(h => h.FechaPartida)
                .Take(20) // Mostramos solo las últimas 20 para no sobrecargar la UI
                .Select(h => new 
                {
                    Juego = h.Juego.ToString(),
                    Resultado = h.Resultado.ToString(),
                    Fecha = h.FechaPartida,
                    Monedas = h.MonedasGanadasOPerdidas
                })
                .ToListAsync();

            return Ok(historial);
        }

        /// <summary>
        /// Requerimiento 12: Ranking global de los mejores jugadores basado en Victorias.
        /// </summary>
        [HttpGet("ranking")]
        public async Task<IActionResult> ObtenerRanking()
        {
            // Agrupamos el historial por usuario, contamos victorias y ordenamos de mayor a menor
            var ranking = await _context.HistorialPartidas
                .Where(h => h.Resultado == ResultadoPartida.Ganador)
                .GroupBy(h => h.Usuario)
                .Select(grupo => new
                {
                    Username = grupo.Key != null ? grupo.Key.Username : "Desconocido",
                    Nivel = grupo.Key != null ? grupo.Key.Nivel : 1,
                    AvatarUrl = grupo.Key != null ? grupo.Key.AvatarUrl : "default_avatar.png",
                    TotalVictorias = grupo.Count()
                })
                .OrderByDescending(r => r.TotalVictorias)
                .Take(10) // Top 10 jugadores
                .ToListAsync();

            return Ok(ranking);
        }

        /// <summary>
        /// Requerimiento 8: Consulta de logros obtenidos por el jugador.
        /// </summary>
        [HttpGet("logros/{usuarioId}")]
        public async Task<IActionResult> ObtenerLogros(int usuarioId)
        {
            var logros = await _context.UsuarioLogros
                .Include(ul => ul.Logro)
                .Where(ul => ul.UsuarioId == usuarioId)
                .Select(ul => new
                {
                    Titulo = ul.Logro != null ? ul.Logro.Titulo : "Logro Desconocido",
                    Descripcion = ul.Logro != null ? ul.Logro.Descripcion : "",
                    Fecha = ul.FechaDesbloqueo
                })
                .ToListAsync();

            return Ok(logros);
        }
    }
}