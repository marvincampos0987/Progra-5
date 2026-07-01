using JuegoClub.Dominio.Entidades;
using JuegoClub.AccesoDatos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace JuegoClub.LogicaNegocios.Implementaciones
{
    public class LogroService
    {
        private readonly JuegoClubContext _context;

        public LogroService(JuegoClubContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Evalúa y desbloquea logros automáticamente tras finalizar una partida.
        /// </summary>
        public async Task ProcesarLogrosPostPartidaAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.LogrosDesbloqueados)
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null) return;

            // Obtener estadísticas del usuario para evaluar condiciones
            var totalVictorias = await _context.HistorialPartidas
                .CountAsync(h => h.UsuarioId == usuarioId && h.Resultado == ResultadoPartida.Ganador);
            
            var logrosExistentes = await _context.Logros.ToListAsync();

            // 1. Logro: "Ganar 5 partidas"
            await ValidarYDesbloquearLogro(usuario, logrosExistentes, "Primera Racha", 
                condicion: totalVictorias >= 5);

            // 2. Logro: "Llegar al nivel 10"
            await ValidarYDesbloquearLogro(usuario, logrosExistentes, "Jugador Experimentado", 
                condicion: usuario.Nivel >= 10);

            await _context.SaveChangesAsync();
        }

        private async Task ValidarYDesbloquearLogro(Usuario usuario, List<Logro> todosLosLogros, string tituloLogro, bool condicion)
        {
            if (!condicion) return;

            var logro = todosLosLogros.FirstOrDefault(l => l.Titulo == tituloLogro);
            if (logro == null) return;

            // Verificar si ya lo tiene desbloqueado
            bool yaDesbloqueado = usuario.LogrosDesbloqueados.Any(ul => ul.LogroId == logro.Id);
            
            if (!yaDesbloqueado)
            {
                usuario.LogrosDesbloqueados.Add(new UsuarioLogro
                {
                    UsuarioId = usuario.Id,
                    LogroId = logro.Id,
                    FechaDesbloqueo = DateTime.UtcNow
                });
                
                // Aquí podrías emitir un evento por SignalR para mostrar una notificación tipo "¡Logro Desbloqueado!" en pantalla
            }
        }
    }
}
