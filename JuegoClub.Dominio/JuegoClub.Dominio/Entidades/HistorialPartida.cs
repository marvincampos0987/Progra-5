namespace JuegoClub.Dominio.Entidades
{
    public enum TipoJuego { Uno, Domino }
    public enum ResultadoPartida { Ganador, SegundoLugar, Perdedor }

    public class HistorialPartida
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public TipoJuego Juego { get; set; }
        public ResultadoPartida Resultado { get; set; }
        public int MonedasApostadas { get; set; } // Apuesta de la mesa
        public int MonedasGanadasOPerdidas { get; set; } // Neto (positivo o negativo)
        public DateTime FechaPartida { get; set; } = DateTime.UtcNow;
    }
}