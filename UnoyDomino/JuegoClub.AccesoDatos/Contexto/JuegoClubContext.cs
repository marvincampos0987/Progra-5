using Microsoft.EntityFrameworkCore;
using JuegoClub.Dominio.Entidades;

namespace JuegoClub.AccesoDatos.Contexto
{
    public class JuegoClubContext : DbContext
    {
        public JuegoClubContext(DbContextOptions<JuegoClubContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<HistorialPartida> HistorialPartidas { get; set; }
        public DbSet<Logro> Logros { get; set; }
        public DbSet<UsuarioLogro> UsuarioLogros { get; set; }
        public DbSet<Articulo> Articulos { get; set; }
        public DbSet<UsuarioArticulo> UsuarioArticulos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la llave compuesta para la tabla intermedia de logros
            modelBuilder.Entity<UsuarioLogro>()
                .HasKey(ul => new { ul.UsuarioId, ul.LogroId });

            // Relaciones
            modelBuilder.Entity<UsuarioLogro>()
                .HasOne(ul => ul.Usuario)
                .WithMany(u => u.LogrosDesbloqueados)
                .HasForeignKey(ul => ul.UsuarioId);

            modelBuilder.Entity<UsuarioLogro>()
                .HasOne(ul => ul.Logro)
                .WithMany()
                .HasForeignKey(ul => ul.LogroId);

            // Configuración de la llave compuesta para la tabla intermedia de artículos
            modelBuilder.Entity<UsuarioArticulo>()
                .HasKey(ua => new { ua.UsuarioId, ua.ArticuloId });

            // Relaciones artículos
            modelBuilder.Entity<UsuarioArticulo>()
                .HasOne(ua => ua.Usuario)
                .WithMany(u => u.ArticulosComprados)
                .HasForeignKey(ua => ua.UsuarioId);

            modelBuilder.Entity<UsuarioArticulo>()
                .HasOne(ua => ua.Articulo)
                .WithMany()
                .HasForeignKey(ua => ua.ArticuloId);
        }
    }
}
