using System;

namespace JuegoClub.Dominio.Entidades
{
    public enum TipoArticulo
    {
        BordeAvatar,
        AspectoTablero,
        SonidoReaccion
    }

    public class Articulo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public TipoArticulo Tipo { get; set; }
        public int Precio { get; set; }
        public string Valor { get; set; } = string.Empty;
    }

    public class UsuarioArticulo
    {
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int ArticuloId { get; set; }
        public Articulo? Articulo { get; set; }

        public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
        public bool Equipado { get; set; } = false;
    }
}
