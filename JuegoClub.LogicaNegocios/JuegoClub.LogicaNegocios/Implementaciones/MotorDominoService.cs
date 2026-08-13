using JuegoClub.Dominio.Clases;

namespace JuegoClub.LogicaNegocios.Implementaciones
{
    public class MotorDominoService
    {
        /// <summary>
        /// Inicializa el juego creando las 28 fichas clÃ¡sicas y repartiendo 7 a cada uno.
        /// </summary>
        public EstadoDomino InicializarJuego(List<string> nombresJugadores)
        {
            var estado = new EstadoDomino();
            var todasLasFichas = GenerarFichasClasicas();

            // Barajar fichas
            var random = new Random();
            var fichasBarajadas = todasLasFichas.OrderBy(f => random.Next()).ToList();

            // Repartir 7 fichas a cada uno (4 jugadores x 7 = 28 fichas exactas)
            foreach (var jugador in nombresJugadores)
            {
                estado.ManosJugadores[jugador] = fichasBarajadas.Take(7).ToList();
                fichasBarajadas.RemoveRange(0, 7);
            }

            estado.Pozo = fichasBarajadas; // En 4 jugadores se quedarÃ¡ vacÃ­o

            return estado;
        }

        /// <summary>
        /// Aplica las reglas automÃ¡ticas del DominÃ³. Valida si una ficha puede entrar y en quÃ© punta.
        /// </summary>
        public bool EsJugadaValida(FichaDomino ficha, EstadoDomino estado, out bool puedeIzquierda, out bool puedeDerecha)
        {
            puedeIzquierda = false;
            puedeDerecha = false;

            // Si el tablero estÃ¡ vacÃ­o, cualquier ficha es vÃ¡lida para abrir la partida
            if (estado.Tablero.Count == 0)
            {
                puedeIzquierda = true;
                puedeDerecha = true;
                return true;
            }

            // Validar coincidencia con la punta izquierda
            if (ficha.LadoA == estado.PuntaIzquierda || ficha.LadoB == estado.PuntaIzquierda)
            {
                puedeIzquierda = true;
            }

            // Validar coincidencia con la punta derecha
            if (ficha.LadoA == estado.PuntaDerecha || ficha.LadoB == estado.PuntaDerecha)
            {
                puedeDerecha = true;
            }

            return puedeIzquierda || puedeDerecha;
        }

        /// <summary>
        /// Ejecuta el turno automÃ¡tico del Bot analizando sus fichas y las puntas libres.
        /// </summary>
        public (FichaDomino? FichaJugada, bool ColocarAInicios) ProcesarTurnoBot(string nombreBot, EstadoDomino estado)
        {
            var manoBot = estado.ManosJugadores[nombreBot];

            foreach (var ficha in manoBot)
            {
                if (EsJugadaValida(ficha, estado, out bool puedeIzquierda, out bool puedeDerecha))
                {
                    // Prioridad del Bot: Si puede jugar a la izquierda, lo hace; si no, a la derecha.
                    return (ficha, puedeIzquierda);
                }
            }

            // Si no tiene jugadas vÃ¡lidas, el Bot "pasa" el turno (retorna null)
            return (null, false);
        }

        /// <summary>
        /// Modifica el estado del tablero rotando la ficha si es necesario para empalmar los nÃºmeros.
        /// </summary>
        public void RealizarJugada(FichaDomino ficha, bool colocarAInicios, EstadoDomino estado)
        {
            if (estado.Tablero.Count == 0)
            {
                estado.Tablero.Add(ficha);
                estado.PuntaIzquierda = ficha.LadoA;
                estado.PuntaDerecha = ficha.LadoB;
                return;
            }

            if (colocarAInicios)
            {
                // Conectar al extremo izquierdo
                if (ficha.LadoB == estado.PuntaIzquierda)
                {
                    estado.PuntaIzquierda = ficha.LadoA;
                }
                else
                {
                    // Voltear la ficha
                    int temp = ficha.LadoA;
                    ficha.LadoA = ficha.LadoB;
                    ficha.LadoB = temp;
                    estado.PuntaIzquierda = ficha.LadoA;
                }
                estado.Tablero.Insert(0, ficha);
            }
            else
            {
                // Conectar al extremo derecho
                if (ficha.LadoA == estado.PuntaDerecha)
                {
                    estado.PuntaDerecha = ficha.LadoB;
                }
                else
                {
                    // Voltear la ficha
                    int temp = ficha.LadoA;
                    ficha.LadoA = ficha.LadoB;
                    ficha.LadoB = temp;
                    estado.PuntaDerecha = ficha.LadoB;
                }
                estado.Tablero.Add(ficha);
            }
        }

        private List<FichaDomino> GenerarFichasClasicas()
        {
            var fichas = new List<FichaDomino>();
            // CombinaciÃ³n Ãºnica del 0 al 6 sin repetir pares inversos (ej: si existe 1-2, no se crea 2-1)
            for (int i = 0; i <= 6; i++)
            {
                for (int j = i; j <= 6; j++)
                {
                    fichas.Add(new FichaDomino { LadoA = i, LadoB = j });
                }
            }
            return fichas;
        }
    }
}