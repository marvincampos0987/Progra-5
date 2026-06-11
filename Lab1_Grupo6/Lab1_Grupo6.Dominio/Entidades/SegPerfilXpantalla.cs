using System;
using System.Collections.Generic;

namespace Lab1_Grupo6.Dominio.Entidades;

public partial class SegPerfilXpantalla
{
    public int PerfilXpantallaId { get; set; }

    public int CodigoPerfil { get; set; }

    public int CodigoPantalla { get; set; }

    public virtual SegPantalla CodigoPantallaNavigation { get; set; } = null!;

    public virtual SegPerfil CodigoPerfilNavigation { get; set; } = null!;
}
