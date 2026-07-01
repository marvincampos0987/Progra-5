using JuegoClub.Dominio.Entidades;

namespace JuegoClub.Dominio.Clases
{
    public enum TipoApuesta { Baja = 100, Media = 500, Alta = 2000, Premium = 5000, Extrema = 10000 }
    public enum EstadoSala { EsperandoJugadores, EnProgreso, Terminada }

    public class Sala
    {
        public string CodigoSala { get; set; } = string.Empty; // CÃ³digo privado de 6 caracteres
        public TipoJuego TipoJuego { get; set; }
        public TipoApuesta Apuesta { get; set; }
        public EstadoSala Estado { get; set; } = EstadoSala.EsperandoJugadores;
        
        public List<JugadorMesa> Jugadores { get; set; } = new List<JugadorMesa>();
        public int TurnoActualIndex { get; set; } = 0;

        // Propiedades dinámicas de las reglas según el juego
        public EstadoUno? EstadoUno { get; set; }
        public EstadoDomino? EstadoDomino { get; set; }

        // Configuración de juego personalizada
        public bool AcumularMas { get; set; } = false;
        public int CartasAcumuladasRobar { get; set; } = 0;
        public bool EsPrivada { get; set; } = true;
    }

    public class JugadorMesa
    {
        public int? UsuarioId { get; set; } // null si es un Bot
        public string Nombre { get; set; } = string.Empty;
        public string? ConnectionId { get; set; } // Identificador de la conexión de SignalR
        public bool EsBot { get; set; }
        public bool EsCreador { get; set; }
    }
}