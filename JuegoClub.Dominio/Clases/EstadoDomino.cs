namespace JuegoClub.Dominio.Clases
{
    public class FichaDomino
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int LadoA { get; set; }
        public int LadoB { get; set; }
        public bool EsDoble => LadoA == LadoB;
    }

    public class EstadoDomino
    {
        public List<FichaDomino> Pozo { get; set; } = new(); // Fichas sobrantes para "robar"
        public List<FichaDomino> Tablero { get; set; } = new(); // Fichas alineadas en la mesa
        public Dictionary<string, List<FichaDomino>> ManosJugadores { get; set; } = new(); // Fichas de cada uno
        
        // Las dos puntas abiertas del tablero de juego
        public int PuntaIzquierda { get; set; } = -1;
        public int PuntaDerecha { get; set; } = -1;
    }
}