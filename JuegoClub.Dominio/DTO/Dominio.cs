namespace JuegoClub.Dominio.DTO
{
    public record RegistroDto(string Username, string Email, string Password);
    
    public record LoginDto(string Email, string Password);

    public record PersonalizarPerfilDto(string AvatarUrl, string ColorPerfil);

    public class PerfilUsuarioDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string ColorPerfil { get; set; } = string.Empty;
        public long Monedas { get; set; }
        public int Nivel { get; set; }
        public int ExperienciaActual { get; set; }
        public int ExperienciaSiguienteNivel { get; set; }
        public double PorcentajeBarraExp => ExperienciaSiguienteNivel > 0 
            ? Math.Round((double)ExperienciaActual / ExperienciaSiguienteNivel * 100, 2) 
            : 0;
    }
}
