namespace JuegoClub.Dominio.Clases
{
    public enum ColorUno { Rojo, Amarillo, Verde, Azul, Comodin }
    public enum ValorUno { Cero, Uno, Dos, Tres, Cuatro, Cinco, Seis, Siete, Ocho, Nueve, MasDos, Reversa, Salto, CambioColor, MasCuatro }

    public class CartaUno
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ColorUno Color { get; set; }
        public ValorUno Valor { get; set; }
    }

    public class EstadoUno
    {
        public List<CartaUno> Mazo { get; set; } = new();
        public List<CartaUno> PozoDescarte { get; set; } = new();
        public Dictionary<string, List<CartaUno>> ManosJugadores { get; set; } = new(); // Key: Nombre o Id del Jugador
        public ColorUno ColorActual { get; set; } // Para cuando se tira un comodÃ­n y cambia el color
        public ValorUno ValorActual { get; set; }
        public bool SentidoHorario { get; set; } = true;
    }
}