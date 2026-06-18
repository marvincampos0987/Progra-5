using JuegoClub.Dominio.Clases;

namespace JuegoClub.LogicaNegocios.Implementaciones
{
    public class MotorUnoService
    {
        /// <summary>
        /// Inicializa el estado del juego con las 108 cartas del UNO clÃ¡sico.
        /// </summary>
        public EstadoUno InicializarJuego(List<string> nombresJugadores)
        {
            var estado = new EstadoUno();
            var mazoClasico = GenerarMazoClasico();
            
            // Barajar
            var random = new Random();
            estado.Mazo = mazoClasico.OrderBy(c => random.Next()).ToList();

            // Repartir 7 cartas a cada uno
            foreach (var jugador in nombresJugadores)
            {
                estado.ManosJugadores[jugador] = estado.Mazo.Take(7).ToList();
                estado.Mazo.RemoveRange(0, 7);
            }

            // Inicializar el Pozo de descarte con la primera carta vÃ¡lida (que no sea comodÃ­n especial)
            var primeraCarta = estado.Mazo.First(c => c.Color != ColorUno.Comodin);
            estado.Mazo.Remove(primeraCarta);
            estado.PozoDescarte.Add(primeraCarta);
            
            estado.ColorActual = primeraCarta.Color;
            estado.ValorActual = primeraCarta.Valor;

            return estado;
        }

        /// <summary>
        /// Aplica automÃ¡ticamente las reglas clÃ¡sicas para validar una jugada.
        /// </summary>
        public bool EsJugadaValida(CartaUno cartaATirar, EstadoUno estado)
        {
            // Un comodÃ­n siempre es vÃ¡lido
            if (cartaATirar.Color == ColorUno.Comodin) return true;

            // Mismo color o mismo valor/nÃºmero
            if (cartaATirar.Color == estado.ColorActual || cartaATirar.Valor == estado.ValorActual) return true;

            return false;
        }

        /// <summary>
        /// Ejecuta el turno automÃ¡tico de un Bot basÃ¡ndose en las reglas.
        /// </summary>
        public (CartaUno? CartaJugada, ColorUno? ColorElegidoPorComodin) ProcesarTurnoBot(string nombreBot, EstadoUno estado)
        {
            var manoBot = estado.ManosJugadores[nombreBot];
            
            // Buscar una carta que pueda jugar
            var cartaValida = manoBot.FirstOrDefault(c => EsJugadaValida(c, estado));

            if (cartaValida != null)
            {
                // Si es un comodÃ­n, el bot elige el color del que tenga mÃ¡s cartas en su mano
                ColorUno? colorElegido = null;
                if (cartaValida.Color == ColorUno.Comodin)
                {
                    colorElegido = manoBot
                        .Where(c => c.Color != ColorUno.Comodin)
                        .GroupBy(c => c.Color)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? ColorUno.Rojo; // Por defecto rojo si solo le quedan comodines
                }

                return (cartaValida, colorElegido);
            }

            // Si no tiene cartas vÃ¡lidas, el sistema retorna null indicando que debe "robar"
            return (null, null);
        }

        private List<CartaUno> GenerarMazoClasico()
        {
            var mazo = new List<CartaUno>();
            var colores = new[] { ColorUno.Rojo, ColorUno.Amarillo, ColorUno.Verde, ColorUno.Azul };

            foreach (var color in colores)
            {
                // Un '0' por color
                mazo.Add(new CartaUno { Color = color, Valor = ValorUno.Cero });
                
                // Dos de cada nÃºmero (1-9) y cartas de acciÃ³n (Salto, Reversa, +2)
                for (int i = 1; i <= 2; i++)
                {
                    for (int n = 1; n <= 9; n++) 
                        mazo.Add(new CartaUno { Color = color, Valor = (ValorUno)n });

                    mazo.Add(new CartaUno { Color = color, Valor = ValorUno.Salto });
                    mazo.Add(new CartaUno { Color = color, Valor = ValorUno.Reversa });
                    mazo.Add(new CartaUno { Color = color, Valor = ValorUno.MasDos });
                }
            }

            // 4 Comodines de Cambio de Color y 4 Comodines de MÃ¡s Cuatro (+4)
            for (int i = 0; i < 4; i++)
            {
                mazo.Add(new CartaUno { Color = ColorUno.Comodin, Valor = ValorUno.CambioColor });
                mazo.Add(new CartaUno { Color = ColorUno.Comodin, Valor = ValorUno.MasCuatro });
            }

            return mazo;
        }
    }
}