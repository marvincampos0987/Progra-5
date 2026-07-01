using JuegoClub.Dominio.Entidades;

namespace JuegoClub.LogicaNegocios.Implementaciones
{
    public class ExperienciaService
    {
        /// <summary>
        /// Otorga experiencia tras una partida y sube de nivel automáticamente si se alcanza el umbral.
        /// Retorna true si el jugador subió de nivel.
        /// </summary>
        public bool OtorgarExperienciaPorPartida(Usuario usuario, ResultadoPartida resultado, out int expGanada)
        {
            expGanada = resultado switch
            {
                ResultadoPartida.Ganador => 150,
                ResultadoPartida.SegundoLugar => 80,
                ResultadoPartida.Perdedor => 30,
                _ => 10
            };

            usuario.ExperienciaActual += expGanada;
            bool subioDeNivel = false;

            // Bucle por si gana tanta experiencia de golpe que sube más de un nivel
            while (usuario.ExperienciaActual >= usuario.ExperienciaSiguienteNivel)
            {
                usuario.ExperienciaActual -= usuario.ExperienciaSiguienteNivel;
                usuario.Nivel++;
                subioDeNivel = true;
            }

            // El progreso y nuevo nivel quedan listos en memoria para que AccesoDatos guarde el usuario
            return subioDeNivel;
        }
    }
}