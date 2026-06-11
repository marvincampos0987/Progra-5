using System;
using System.Collections.Generic;

namespace Lab1_Grupo6.Dominio.Entidades;

public partial class Cliente
{
    public int ClienteId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Email { get; set; }

    public string? Telefono { get; set; }

    public DateTime FechaRegistro { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public int TipoCedula { get; set; }

    public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();

    public virtual TipoCedula TipoCedulaNavigation { get; set; } = null!;
}
