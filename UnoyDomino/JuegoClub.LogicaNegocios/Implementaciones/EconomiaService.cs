using JuegoClub.Dominio.Entidades;
using JuegoClub.Dominio.Clases;
using JuegoClub.Dominio.InterfacesAD; // Asumiendo que aquí tienes tus repositorios o unidad de trabajo

namespace JuegoClub.LogicaNegocios.Implementaciones
{
    public class EconomiaService
    {
        // En tu arquitectura real, aquí inyectarías el repositorio genérico o el DbContext
        // private readonly IRepository<Usuario> _usuarioRepo; 
        // public EconomiaService(IRepository<Usuario> usuarioRepo) { _usuarioRepo = usuarioRepo; }

        /// <summary>
        /// Requerimiento 3: Valida si todos los jugadores reales tienen saldo suficiente para ingresar a la mesa.
        /// </summary>
        public bool ValidarSaldosParaApuesta(List<JugadorMesa> jugadores, TipoApuesta apuesta, out List<string> jugadoresInsuficientes)
        {
            jugadoresInsuficientes = new List<string>();
            int montoApuesta = (int)apuesta;

            foreach (var jugador in jugadores.Where(j => !j.EsBot))
            {
                // TODO: Simulación de consulta a la base de datos. En producción: var usuario = _usuarioRepo.GetById(jugador.UsuarioId);
                long monedasActuales = 1000; // Mock de prueba

                if (monedasActuales < montoApuesta)
                {
                    jugadoresInsuficientes.Add(jugador.Nombre);
                }
            }

            return jugadoresInsuficientes.Count == 0;
        }

        /// <summary>
        /// Requerimiento 3: Distribuye las monedas al finalizar la partida.
        /// El ganador recibe el pozo acumulado de los perdedores.
        /// El segundo lugar recupera exactamente su inversión inicial.
        /// </summary>
        public Dictionary<string, int> ProcesarPremiosFinales(List<JugadorMesa> posicionesFinales, TipoApuesta apuesta)
        {
            int montoApuesta = (int)apuesta;
            var balanceNetoPorJugador = new Dictionary<string, int>();

            // En una mesa de 4 jugadores, el pozo total inicial recolectado es: montoApuesta * 4
            // Pero como el 2º lugar recupera su inversión, el pozo neto en juego para el 1º lugar es: montoApuesta * 3
            
            for (int i = 0; i < posicionesFinales.Count; i++)
            {
                var jugador = posicionesFinales[i];
                if (jugador.EsBot) continue; // Los bots no alteran la base de datos real

                int neto = 0;
                if (i == 0) // 1er Lugar (Ganador)
                {
                    neto = montoApuesta * 3; // Gana la apuesta de los otros 3 (menos el que recuperó)
                }
                else if (i == 1) // 2do Lugar
                {
                    neto = 0; // Recupera su inversión, ganancia neta = 0
                }
                else // 3er y 4to Lugar (Perdedores)
                {
                    neto = -montoApuesta; // Pierden la totalidad de la apuesta apostada
                }

                balanceNetoPorJugador[jugador.Nombre] = neto;

                // TODO: Persistir en Base de Datos:
                // var usuario = _usuarioRepo.GetById(jugador.UsuarioId);
                // usuario.Monedas += neto;
                // _usuarioRepo.Update(usuario);
            }

            return balanceNetoPorJugador;
        }

        /// <summary>
        /// Requerimiento 4: Permite reclamar la recompensa diaria cada 24 horas exactas si cumple el tiempo.
        /// </summary>
        public bool ReclamarRecompensaDiaria(Usuario usuario, out string mensaje)
        {
            const int MonedasRecompensa = 500;
            var ahora = DateTime.UtcNow;

            if (usuario.UltimaRecompensaDiaria.HasValue && (ahora - usuario.UltimaRecompensaDiaria.Value).TotalHours < 24)
            {
                var tiempoRestante = usuario.UltimaRecompensaDiaria.Value.AddHours(24) - ahora;
                mensaje = $"Debes esperar {tiempoRestante.Hours}h y {tiempoRestante.Minutes}m para volver a reclamar.";
                return false;
            }

            usuario.Monedas += MonedasRecompensa;
            usuario.UltimaRecompensaDiaria = ahora;
            
            // TODO: Guardar cambios en base de datos de AccesoDatos
            mensaje = $"¡Éxito! Has reclamado {MonedasRecompensa} monedas gratis.";
            return true;
        }

        /// <summary>
        /// Requerimiento 4: Auxilio cuando el jugador llega a 0 monedas.
        /// </summary>
        public void ReclamarAuxilioPorBancarrota(Usuario usuario)
        {
            if (usuario.Monedas <= 0)
            {
                usuario.Monedas = 200; // Auxilio mínimo para volver a ingresar a mesas de apuesta Baja (100)
                // TODO: Guardar cambios
            }
        }
    }
}