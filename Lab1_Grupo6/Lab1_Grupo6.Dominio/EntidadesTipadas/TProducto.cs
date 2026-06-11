using Lab1_Grupo6.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1_Grupo6.Dominio.EntidadesTipadas
{
    public class TProducto
    {
        public string ProductoId { get; set; } = null!;

        public string Nombre { get; set; } = null!;

        public decimal Precio { get; set; }

        public int Stock { get; set; }

        public int CategoriaId { get; set; }

        public bool Activo { get; set; }

        public DateTime CreadoEn { get; set; }

        public string? CreadoPor { get; set; }

        public DateTime? ActualizadoEn { get; set; }

        public string? ActualizadoPor { get; set; }

        public byte[] RowVer { get; set; } = null!;

        public virtual Categorium Categoria { get; set; } = null!;

        public virtual ICollection<DetallesPedido> DetallesPedidos { get; set; } = new List<DetallesPedido>();
    }
}
