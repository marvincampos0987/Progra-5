using System;
using System.Collections.Generic;

namespace Lab1_Grupo6.Dominio.Entidades;

public partial class Categorium
{
    public int CategoriaId { get; set; }

    public string NombreCategoria { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public string? CreadoPor { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public string? ActualizadoPor { get; set; }

    public byte[] RowVer { get; set; } = null!;

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
