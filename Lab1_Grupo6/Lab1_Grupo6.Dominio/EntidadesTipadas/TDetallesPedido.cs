using Lab1_Grupo6.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lab1_Grupo6.Dominio.EntidadesTipadas
{
    public class DetallesPedido
    {
        public int DetalleId { get; set; }

        public int PedidoId { get; set; }

        public string ProductoId { get; set; } = null!;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Descuento { get; set; }

        public virtual Pedido Pedido { get; set; } = null!;

        public virtual Producto Producto { get; set; } = null!;
    }
}
