using System;
using System.Collections.Generic;

namespace Lab1_Grupo6.Dominio.Entidades;

public partial class SegPantalla
{
    public int CodigoPantalla { get; set; }

    public string NombrePantalla { get; set; } = null!;

    public int Posicion { get; set; }

    public virtual ICollection<SegPerfilXpantalla> SegPerfilXpantallas { get; set; } = new List<SegPerfilXpantalla>();
}
