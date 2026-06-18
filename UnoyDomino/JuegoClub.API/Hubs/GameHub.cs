using Microsoft.AspNetCore.SignalR;
using JuegoClub.Dominio.Clases;
using JuegoClub.Dominio.Entidades;
using JuegoClub.LogicaNegocios.Implementaciones;
using System.Security.Claims;

namespace JuegoClub.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly MotorUnoService _motorUno;
        private readonly MotorDominoService _motorDomino;

        public GameHub(MotorUnoService motorUno, MotorDominoService motorDomino)
        {
            _motorUno = motorUno;
            _motorDomino = motorDomino;
        }

        private static readonly Dictionary<string, Sala> SalasActivas = new();

        /// <summary>
        /// El jugador crea una nueva sala privada con una apuesta específica.
        /// </summary>
        public async Task CrearSala(TipoJuego juego, TipoApuesta apuesta)
        {
            string codigoSala = GenerarCodigoSala();
            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            var nuevaSala = new Sala
            {
                CodigoSala = codigoSala,
                TipoJuego = juego,
                Apuesta = apuesta,
                Estado = EstadoSala.EsperandoJugadores
            };

            var creador = new JugadorMesa
            {
                UsuarioId = usuarioId,
                Nombre = username,
                ConnectionId = Context.ConnectionId,
                EsBot = false,
                EsCreador = true
            };

            nuevaSala.Jugadores.Add(creador);
            SalasActivas[codigoSala] = nuevaSala;

            await Groups.AddToGroupAsync(Context.ConnectionId, codigoSala);
            await Clients.Caller.SendAsync("SalaCreada", nuevaSala);
        }

        /// <summary>
        /// Un jugador se une a una sala existente usando un código de 6 caracteres.
        /// </summary>
        public async Task UnirseSala(string codigoSala)
        {
            codigoSala = codigoSala.ToUpper().Trim();

            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            if (sala.Estado != EstadoSala.EsperandoJugadores)
            {
                await Clients.Caller.SendAsync("Error", "La partida ya ha comenzado o finalizado.");
                return;
            }

            if (sala.Jugadores.Count >= 4)
            {
                await Clients.Caller.SendAsync("Error", "La sala está llena.");
                return;
            }

            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            if (sala.Jugadores.Any(j => j.UsuarioId == usuarioId && usuarioId != null))
            {
                await Clients.Caller.SendAsync("Error", "Ya estás unido a esta sala.");
                return;
            }

            var nuevoJugador = new JugadorMesa
            {
                UsuarioId = usuarioId,
                Nombre = username,
                ConnectionId = Context.ConnectionId,
                EsBot = false,
                EsCreador = false
            };

            sala.Jugadores.Add(nuevoJugador);
            await Groups.AddToGroupAsync(Context.ConnectionId, codigoSala);

            await Clients.Group(codigoSala).SendAsync("SalaActualizada", sala);
        }

        /// <summary>
        /// El creador inicia la partida. Si faltan jugadores, se completan automáticamente con Bots.
        /// </summary>
        public async Task IniciarPartida(string codigoSala)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            var jugadorSolicitante = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            if (jugadorSolicitante == null || !jugadorSolicitante.EsCreador)
            {
                await Clients.Caller.SendAsync("Error", "Solo el creador de la sala puede iniciar la partida.");
                return;
            }

            int botContador = 1;
            while (sala.Jugadores.Count < 4)
            {
                sala.Jugadores.Add(new JugadorMesa
                {
                    UsuarioId = null,
                    Nombre = $"Bot_{botContador++}",
                    ConnectionId = null,
                    EsBot = true,
                    EsCreador = false
                });
            }

            var nombresJugadores = sala.Jugadores.Select(j => j.Nombre).ToList();

            if (sala.TipoJuego == TipoJuego.Uno)
            {
                sala.EstadoUno = _motorUno.InicializarJuego(nombresJugadores);
            }
            else if (sala.TipoJuego == TipoJuego.Domino)
            {
                sala.EstadoDomino = _motorDomino.InicializarJuego(nombresJugadores);
            }

            sala.Estado = EstadoSala.EnProgreso;
            sala.TurnoActualIndex = 0;

            await Clients.Group(codigoSala).SendAsync("PartidaIniciada");
            await EnviarEstadoRedactado(sala);

            await VerificarYProcesarTurnoBot(sala);
        }

        /// <summary>
        /// Requisito 9: Chat rápido / Emojis durante la partida
        /// </summary>
        public async Task EnviarChatRapido(string codigoSala, int mensajeId)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala)) return;
            var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            string username = jugador?.Nombre ?? "Anónimo";
            await Clients.Group(codigoSala).SendAsync("RecibirChatRapido", username, mensajeId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var sala in SalasActivas.Values)
            {
                var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                if (jugador != null)
                {
                    if (sala.Estado == EstadoSala.EsperandoJugadores)
                    {
                        sala.Jugadores.Remove(jugador);
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala.CodigoSala);
                        await Clients.Group(sala.CodigoSala).SendAsync("SalaActualizada", sala);
                    }
                    else if (sala.Estado == EstadoSala.EnProgreso)
                    {
                        jugador.EsBot = true;
                        jugador.ConnectionId = null;
                        await Clients.Group(sala.CodigoSala).SendAsync("JugadorSeConvirtióEnBot", jugador.Nombre);
                        await EnviarEstadoRedactado(sala);
                        await VerificarYProcesarTurnoBot(sala);
                    }
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        #region Acciones del Jugador Humano

        public async Task JugarCartaUno(string codigoSala, Guid cartaId, ColorUno? colorElegidoPorComodin)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Uno || sala.EstadoUno == null)
            {
                await Clients.Caller.SendAsync("Error", "El juego de UNO no está en progreso.");
                return;
            }

            var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            if (jugador == null)
            {
                await Clients.Caller.SendAsync("Error", "No eres parte de esta sala.");
                return;
            }

            if (jugador.Nombre != jugadorActual.Nombre)
            {
                await Clients.Caller.SendAsync("Error", "No es tu turno.");
                return;
            }

            var mano = sala.EstadoUno.ManosJugadores[jugador.Nombre];
            var carta = mano.FirstOrDefault(c => c.Id == cartaId);
            if (carta == null)
            {
                await Clients.Caller.SendAsync("Error", "No tienes esa carta en tu mano.");
                return;
            }

            if (!_motorUno.EsJugadaValida(carta, sala.EstadoUno))
            {
                await Clients.Caller.SendAsync("Error", "Jugada no válida.");
                return;
            }

            mano.Remove(carta);
            sala.EstadoUno.PozoDescarte.Add(carta);
            sala.EstadoUno.ColorActual = (carta.Color == ColorUno.Comodin) ? (colorElegidoPorComodin ?? ColorUno.Rojo) : carta.Color;
            sala.EstadoUno.ValorActual = carta.Valor;

            bool saltarSiguiente = false;
            if (carta.Valor == ValorUno.Reversa)
            {
                sala.EstadoUno.SentidoHorario = !sala.EstadoUno.SentidoHorario;
            }
            else if (carta.Valor == ValorUno.Salto)
            {
                saltarSiguiente = true;
            }
            else if (carta.Valor == ValorUno.MasDos || carta.Valor == ValorUno.MasCuatro)
            {
                int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                int siguienteIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                var siguienteJugador = sala.Jugadores[siguienteIndex];
                int cantidadARobar = (carta.Valor == ValorUno.MasDos) ? 2 : 4;
                
                for (int i = 0; i < cantidadARobar; i++)
                {
                    RobaCartaParaJugador(sala, siguienteJugador.Nombre);
                }
                saltarSiguiente = true;
            }

            await Clients.Group(codigoSala).SendAsync("CartaJugadaUno", jugador.Nombre, carta, sala.EstadoUno.ColorActual);

            if (mano.Count == 0)
            {
                sala.Estado = EstadoSala.Terminada;
                await Clients.Group(codigoSala).SendAsync("PartidaTerminada", jugador.Nombre);
                return;
            }

            int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
            if (saltarSiguiente)
            {
                sala.TurnoActualIndex = (sala.TurnoActualIndex + 2 * pasoTurno + 4) % 4;
            }
            else
            {
                sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
            }

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            await VerificarYProcesarTurnoBot(sala);
        }

        public async Task RobarCartaUno(string codigoSala)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Uno || sala.EstadoUno == null)
            {
                await Clients.Caller.SendAsync("Error", "El juego de UNO no está en progreso.");
                return;
            }

            var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            if (jugador == null)
            {
                await Clients.Caller.SendAsync("Error", "No eres parte de esta sala.");
                return;
            }

            if (jugador.Nombre != jugadorActual.Nombre)
            {
                await Clients.Caller.SendAsync("Error", "No es tu turno.");
                return;
            }

            var cartaRobada = RobaCartaParaJugador(sala, jugador.Nombre);
            if (cartaRobada != null)
            {
                await Clients.Group(codigoSala).SendAsync("CartaRobadaUno", jugador.Nombre);
            }

            if (cartaRobada != null && _motorUno.EsJugadaValida(cartaRobada, sala.EstadoUno))
            {
                await EnviarEstadoRedactado(sala);
            }
            else
            {
                int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                sala.TurnoActualIndex = (sala.TurnoActualIndex + paso + 4) % 4;

                await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
                await EnviarEstadoRedactado(sala);

                await VerificarYProcesarTurnoBot(sala);
            }
        }

        public async Task JugarFichaDomino(string codigoSala, Guid fichaId, bool colocarAInicios)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Domino || sala.EstadoDomino == null)
            {
                await Clients.Caller.SendAsync("Error", "El juego de Dominó no está en progreso.");
                return;
            }

            var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            if (jugador == null)
            {
                await Clients.Caller.SendAsync("Error", "No eres parte de esta sala.");
                return;
            }

            if (jugador.Nombre != jugadorActual.Nombre)
            {
                await Clients.Caller.SendAsync("Error", "No es tu turno.");
                return;
            }

            var mano = sala.EstadoDomino.ManosJugadores[jugador.Nombre];
            var ficha = mano.FirstOrDefault(f => f.Id == fichaId);
            if (ficha == null)
            {
                await Clients.Caller.SendAsync("Error", "No tienes esa ficha en tu mano.");
                return;
            }

            if (!_motorDomino.EsJugadaValida(ficha, sala.EstadoDomino, out bool puedeIzquierda, out bool puedeDerecha))
            {
                await Clients.Caller.SendAsync("Error", "Jugada no válida.");
                return;
            }

            if (colocarAInicios && !puedeIzquierda)
            {
                await Clients.Caller.SendAsync("Error", "No se puede colocar al inicio.");
                return;
            }
            if (!colocarAInicios && !puedeDerecha)
            {
                await Clients.Caller.SendAsync("Error", "No se puede colocar al final.");
                return;
            }

            mano.Remove(ficha);
            _motorDomino.RealizarJugada(ficha, colocarAInicios, sala.EstadoDomino);

            await Clients.Group(codigoSala).SendAsync("FichaJugadaDomino", jugador.Nombre, ficha, colocarAInicios);

            if (mano.Count == 0)
            {
                sala.Estado = EstadoSala.Terminada;
                await Clients.Group(codigoSala).SendAsync("PartidaTerminada", jugador.Nombre);
                return;
            }

            sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            await VerificarYProcesarTurnoBot(sala);
        }

        public async Task PasarORobarTurnoDomino(string codigoSala)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Domino || sala.EstadoDomino == null)
            {
                await Clients.Caller.SendAsync("Error", "El juego de Dominó no está en progreso.");
                return;
            }

            var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
            if (jugador == null)
            {
                await Clients.Caller.SendAsync("Error", "No eres parte de esta sala.");
                return;
            }

            if (jugador.Nombre != jugadorActual.Nombre)
            {
                await Clients.Caller.SendAsync("Error", "No es tu turno.");
                return;
            }

            var mano = sala.EstadoDomino.ManosJugadores[jugador.Nombre];

            bool tieneJugadasValidas = mano.Any(f => _motorDomino.EsJugadaValida(f, sala.EstadoDomino, out _, out _));
            if (tieneJugadasValidas)
            {
                await Clients.Caller.SendAsync("Error", "Tienes fichas válidas que puedes jugar.");
                return;
            }

            if (sala.EstadoDomino.Pozo.Count > 0)
            {
                var fichaRobada = sala.EstadoDomino.Pozo.First();
                sala.EstadoDomino.Pozo.Remove(fichaRobada);
                mano.Add(fichaRobada);

                await Clients.Group(codigoSala).SendAsync("FichaRobadaDomino", jugador.Nombre);

                if (_motorDomino.EsJugadaValida(fichaRobada, sala.EstadoDomino, out _, out _))
                {
                    await EnviarEstadoRedactado(sala);
                    return; 
                }
            }

            await Clients.Group(codigoSala).SendAsync("JugadorPasóTurnoDomino", jugador.Nombre);
            sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            await VerificarYProcesarTurnoBot(sala);
        }

        #endregion

        #region Métodos de Soporte Interno

        private async Task EnviarEstadoRedactado(Sala sala)
        {
            foreach (var jugador in sala.Jugadores)
            {
                if (jugador.EsBot || string.IsNullOrEmpty(jugador.ConnectionId)) continue;

                var estadoRedactado = ObtenerVistaRedactada(sala, jugador.Nombre);
                await Clients.Client(jugador.ConnectionId).SendAsync("EstadoPartidaActualizado", estadoRedactado);
            }
        }

        private object ObtenerVistaRedactada(Sala sala, string nombreDestinatario)
        {
            object? estadoUnoRedactado = null;
            if (sala.EstadoUno != null)
            {
                var manosRedactadas = sala.EstadoUno.ManosJugadores.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Key == nombreDestinatario 
                        ? (object)kvp.Value 
                        : (object)new { Cantidad = kvp.Value.Count }
                );

                estadoUnoRedactado = new
                {
                    MazoCount = sala.EstadoUno.Mazo.Count,
                    PozoDescarte = sala.EstadoUno.PozoDescarte,
                    ManosJugadores = manosRedactadas,
                    ColorActual = sala.EstadoUno.ColorActual,
                    ValorActual = sala.EstadoUno.ValorActual,
                    SentidoHorario = sala.EstadoUno.SentidoHorario
                };
            }

            object? estadoDominoRedactado = null;
            if (sala.EstadoDomino != null)
            {
                var manosRedactadas = sala.EstadoDomino.ManosJugadores.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Key == nombreDestinatario 
                        ? (object)kvp.Value 
                        : (object)new { Cantidad = kvp.Value.Count }
                );

                estadoDominoRedactado = new
                {
                    PozoCount = sala.EstadoDomino.Pozo.Count,
                    Tablero = sala.EstadoDomino.Tablero,
                    ManosJugadores = manosRedactadas,
                    PuntaIzquierda = sala.EstadoDomino.PuntaIzquierda,
                    PuntaDerecha = sala.EstadoDomino.PuntaDerecha
                };
            }

            return new
            {
                CodigoSala = sala.CodigoSala,
                TipoJuego = sala.TipoJuego,
                Apuesta = sala.Apuesta,
                Estado = sala.Estado,
                Jugadores = sala.Jugadores.Select(j => new
                {
                    j.Nombre,
                    j.EsBot,
                    j.EsCreador
                }),
                TurnoActualIndex = sala.TurnoActualIndex,
                EstadoUno = estadoUnoRedactado,
                EstadoDomino = estadoDominoRedactado
            };
        }

        private CartaUno? RobaCartaParaJugador(Sala sala, string nombreJugador)
        {
            if (sala.EstadoUno == null) return null;
            if (sala.EstadoUno.Mazo.Count == 0)
            {
                var topCard = sala.EstadoUno.PozoDescarte.LastOrDefault();
                if (topCard != null)
                {
                    sala.EstadoUno.PozoDescarte.Remove(topCard);
                }

                if (sala.EstadoUno.PozoDescarte.Count > 0)
                {
                    var random = new Random();
                    sala.EstadoUno.Mazo = sala.EstadoUno.PozoDescarte.OrderBy(c => random.Next()).ToList();
                    sala.EstadoUno.PozoDescarte.Clear();
                }

                if (topCard != null)
                {
                    sala.EstadoUno.PozoDescarte.Add(topCard);
                }
            }

            if (sala.EstadoUno.Mazo.Count > 0)
            {
                var carta = sala.EstadoUno.Mazo.First();
                sala.EstadoUno.Mazo.Remove(carta);
                sala.EstadoUno.ManosJugadores[nombreJugador].Add(carta);
                return carta;
            }

            return null;
        }

        private async Task VerificarYProcesarTurnoBot(Sala sala)
        {
            if (sala.Estado != EstadoSala.EnProgreso) return;

            var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            if (!jugadorActual.EsBot) return;

            await Task.Delay(1500);

            if (sala.TipoJuego == TipoJuego.Uno && sala.EstadoUno != null)
            {
                var resultado = _motorUno.ProcesarTurnoBot(jugadorActual.Nombre, sala.EstadoUno);

                if (resultado.CartaJugada != null)
                {
                    sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Remove(resultado.CartaJugada);
                    sala.EstadoUno.PozoDescarte.Add(resultado.CartaJugada);
                    sala.EstadoUno.ColorActual = (resultado.CartaJugada.Color == ColorUno.Comodin) 
                        ? (resultado.ColorElegidoPorComodin ?? ColorUno.Rojo) 
                        : resultado.CartaJugada.Color;
                    sala.EstadoUno.ValorActual = resultado.CartaJugada.Valor;

                    bool saltarSiguiente = false;
                    if (resultado.CartaJugada.Valor == ValorUno.Reversa)
                    {
                        sala.EstadoUno.SentidoHorario = !sala.EstadoUno.SentidoHorario;
                    }
                    else if (resultado.CartaJugada.Valor == ValorUno.Salto)
                    {
                        saltarSiguiente = true;
                    }
                    else if (resultado.CartaJugada.Valor == ValorUno.MasDos || resultado.CartaJugada.Valor == ValorUno.MasCuatro)
                    {
                        int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                        int siguienteIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                        var siguienteJugador = sala.Jugadores[siguienteIndex];
                        int cantidadARobar = (resultado.CartaJugada.Valor == ValorUno.MasDos) ? 2 : 4;
                        for (int i = 0; i < cantidadARobar; i++)
                        {
                            RobaCartaParaJugador(sala, siguienteJugador.Nombre);
                        }
                        saltarSiguiente = true;
                    }

                    await Clients.Group(sala.CodigoSala).SendAsync("BotJugóUno", jugadorActual.Nombre, resultado.CartaJugada, sala.EstadoUno.ColorActual);

                    if (sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Count == 0)
                    {
                        sala.Estado = EstadoSala.Terminada;
                        await Clients.Group(sala.CodigoSala).SendAsync("PartidaTerminada", jugadorActual.Nombre);
                        return;
                    }

                    int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                    if (saltarSiguiente)
                    {
                        sala.TurnoActualIndex = (sala.TurnoActualIndex + 2 * pasoTurno + 4) % 4;
                    }
                    else
                    {
                        sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                    }
                }
                else
                {
                    var cartaRobada = RobaCartaParaJugador(sala, jugadorActual.Nombre);
                    if (cartaRobada != null)
                    {
                        await Clients.Group(sala.CodigoSala).SendAsync("BotRobóCarta", jugadorActual.Nombre);

                        if (_motorUno.EsJugadaValida(cartaRobada, sala.EstadoUno))
                        {
                            sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Remove(cartaRobada);
                            sala.EstadoUno.PozoDescarte.Add(cartaRobada);
                            
                            ColorUno colorElegido = cartaRobada.Color;
                            if (cartaRobada.Color == ColorUno.Comodin)
                            {
                                var mano = sala.EstadoUno.ManosJugadores[jugadorActual.Nombre];
                                colorElegido = mano.Where(c => c.Color != ColorUno.Comodin)
                                                   .GroupBy(c => c.Color)
                                                   .OrderByDescending(g => g.Count())
                                                   .FirstOrDefault()?.Key ?? ColorUno.Rojo;
                            }

                            sala.EstadoUno.ColorActual = colorElegido;
                            sala.EstadoUno.ValorActual = cartaRobada.Valor;

                            bool saltarSiguienteDrawn = false;
                            if (cartaRobada.Valor == ValorUno.Reversa)
                            {
                                sala.EstadoUno.SentidoHorario = !sala.EstadoUno.SentidoHorario;
                            }
                            else if (cartaRobada.Valor == ValorUno.Salto)
                            {
                                saltarSiguienteDrawn = true;
                            }
                            else if (cartaRobada.Valor == ValorUno.MasDos || cartaRobada.Valor == ValorUno.MasCuatro)
                            {
                                int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                int siguienteIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                                var siguienteJugador = sala.Jugadores[siguienteIndex];
                                int cantidadARobar = (cartaRobada.Valor == ValorUno.MasDos) ? 2 : 4;
                                for (int i = 0; i < cantidadARobar; i++)
                                {
                                    RobaCartaParaJugador(sala, siguienteJugador.Nombre);
                                }
                                saltarSiguienteDrawn = true;
                            }

                            await Clients.Group(sala.CodigoSala).SendAsync("BotJugóUno", jugadorActual.Nombre, cartaRobada, sala.EstadoUno.ColorActual);

                            if (sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Count == 0)
                            {
                                sala.Estado = EstadoSala.Terminada;
                                await Clients.Group(sala.CodigoSala).SendAsync("PartidaTerminada", jugadorActual.Nombre);
                                return;
                            }

                            int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                            if (saltarSiguienteDrawn)
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + 2 * pasoTurno + 4) % 4;
                            }
                            else
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                            }
                        }
                        else
                        {
                            int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                            sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                        }
                    }
                    else
                    {
                        int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                        sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                    }
                }
            }
            else if (sala.TipoJuego == TipoJuego.Domino && sala.EstadoDomino != null)
            {
                var resultado = _motorDomino.ProcesarTurnoBot(jugadorActual.Nombre, sala.EstadoDomino);

                if (resultado.FichaJugada != null)
                {
                    sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Remove(resultado.FichaJugada);
                    _motorDomino.RealizarJugada(resultado.FichaJugada, resultado.ColocarAInicios, sala.EstadoDomino);

                    await Clients.Group(sala.CodigoSala).SendAsync("BotJugóDomino", jugadorActual.Nombre, resultado.FichaJugada, resultado.ColocarAInicios);

                    if (sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Count == 0)
                    {
                        sala.Estado = EstadoSala.Terminada;
                        await Clients.Group(sala.CodigoSala).SendAsync("PartidaTerminada", jugadorActual.Nombre);
                        return;
                    }

                    sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                }
                else
                {
                    if (sala.EstadoDomino.Pozo.Count > 0)
                    {
                        var fichaRobada = sala.EstadoDomino.Pozo.First();
                        sala.EstadoDomino.Pozo.Remove(fichaRobada);
                        sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Add(fichaRobada);

                        await Clients.Group(sala.CodigoSala).SendAsync("BotRobóDomino", jugadorActual.Nombre);

                        if (_motorDomino.EsJugadaValida(fichaRobada, sala.EstadoDomino, out bool puedeIzquierda, out bool puedeDerecha))
                        {
                            sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Remove(fichaRobada);
                            _motorDomino.RealizarJugada(fichaRobada, puedeIzquierda, sala.EstadoDomino);

                            await Clients.Group(sala.CodigoSala).SendAsync("BotJugóDomino", jugadorActual.Nombre, fichaRobada, puedeIzquierda);

                            if (sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Count == 0)
                            {
                                sala.Estado = EstadoSala.Terminada;
                                await Clients.Group(sala.CodigoSala).SendAsync("PartidaTerminada", jugadorActual.Nombre);
                                return;
                            }
                        }
                    }
                    else
                    {
                        await Clients.Group(sala.CodigoSala).SendAsync("BotPasóTurnoDomino", jugadorActual.Nombre);
                    }

                    sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                }
            }

            await Clients.Group(sala.CodigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            await VerificarYProcesarTurnoBot(sala);
        }

        #endregion

        #region Métodos Auxiliares de Utilidad

        private static string GenerarCodigoSala()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, 6).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private int? ObtenerUsuarioIdDesdeContexto()
        {
            var claimId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claimId, out int id) ? id : null;
        }

        #endregion
    }
}