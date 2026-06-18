namespace JuegoClub.Dominio.Entidades
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
        
        // PersonalizaciÃ³n y Cuenta
        public string AvatarUrl { get; set; } = "default_avatar.png";
        public string ColorPerfil { get; set; } = "#3498db"; // Color en formato HEX
        
        // EconomÃ­a y Niveles
        public long Monedas { get; set; } = 1000; // Monedas iniciales
        public int Nivel { get; set; } = 1;
        public int ExperienciaActual { get; set; } = 0;
        public int ExperienciaSiguienteNivel => Nivel * 100; // FÃ³rmula simple de escalado

        // Control de RecuperaciÃ³n de Monedas
        public DateTime? UltimaRecompensaDiaria { get; set; }

        // Relaciones
        public ICollection<HistorialPartida> HistorialPartidas { get; set; } = new List<HistorialPartida>();
        public ICollection<UsuarioLogro> LogrosDesbloqueados { get; set; } = new List<UsuarioLogro>();
    }
}