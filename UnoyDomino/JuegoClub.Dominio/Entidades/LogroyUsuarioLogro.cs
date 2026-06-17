namespace JuegoClub.Dominio.Entidades
{
    public class Logro
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int PuntosExperienciaRecompensa { get; set; }
    }

    public class UsuarioLogro
    {
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int LogroId { get; set; }
        public Logro? Logro { get; set; }

        public DateTime FechaDesbloqueo { get; set; } = DateTime.UtcNow;
    }
}