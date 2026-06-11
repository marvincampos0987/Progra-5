using System;
using System.Collections.Generic;

namespace Lab1_Grupo6.Dominio.Entidades;

public partial class Pedido
{
    public int PedidoId { get; set; }

    public int ClienteId { get; set; }

    public DateTime FechaPedido { get; set; }

    public string Moneda { get; set; } = null!;

    public decimal? Total { get; set; }

    public DateTime CreadoEn { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<DetallesPedido> DetallesPedidos { get; set; } = new List<DetallesPedido>();
}
