using Microsoft.AspNetCore.SignalR;
using JuegoClub.Dominio.Clases;
using JuegoClub.Dominio.Entidades;
using JuegoClub.AccesoDatos.Contexto;
using JuegoClub.LogicaNegocios.Implementaciones;
using System.Security.Claims;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace JuegoClub.API.Hubs
{
    public class GameHub : Hub
    {
        private readonly MotorUnoService _motorUno;
        private readonly MotorDominoService _motorDomino;
        private readonly JuegoClubContext _context;
        private readonly ExperienciaService _experienciaService;
        private readonly LogroService _logroService;

        public GameHub(
            MotorUnoService motorUno, 
            MotorDominoService motorDomino,
            JuegoClubContext context,
            ExperienciaService experienciaService,
            LogroService logroService)
        {
            _motorUno = motorUno;
            _motorDomino = motorDomino;
            _context = context;
            _experienciaService = experienciaService;
            _logroService = logroService;
        }

        private static readonly ConcurrentDictionary<string, Sala> SalasActivas = new();

        /// <summary>
        /// El jugador crea una nueva sala privada con una apuesta específica y reglas personalizadas.
        /// </summary>
        public async Task CrearSala(TipoJuego juego, TipoApuesta apuesta, bool acumularMas)
        {
            string codigoSala = GenerarCodigoSala();
            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            var nuevaSala = new Sala
            {
                CodigoSala = codigoSala,
                TipoJuego = juego,
                Apuesta = apuesta,
                Estado = EstadoSala.EsperandoJugadores,
                AcumularMas = acumularMas
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

            while (!SalasActivas.TryAdd(codigoSala, nuevaSala))
            {
                codigoSala = GenerarCodigoSala();
                nuevaSala.CodigoSala = codigoSala;
            }

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

            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            string? errorMessage = null;
            var nuevoJugador = new JugadorMesa
            {
                UsuarioId = usuarioId,
                Nombre = username,
                ConnectionId = Context.ConnectionId,
                EsBot = false,
                EsCreador = false
            };

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EsperandoJugadores)
                {
                    errorMessage = "La partida ya ha comenzado o finalizado.";
                }
                else if (sala.Jugadores.Count >= 4)
                {
                    errorMessage = "La sala está llena.";
                }
                else if (sala.Jugadores.Any(j => j.UsuarioId == usuarioId && usuarioId != null))
                {
                    errorMessage = "Ya estás unido a esta sala.";
                }
                else
                {
                    sala.Jugadores.Add(nuevoJugador);
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

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

            string? errorMessage = null;

            lock (sala)
            {
                var jugadorSolicitante = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                if (jugadorSolicitante == null || !jugadorSolicitante.EsCreador)
                {
                    errorMessage = "Solo el creador de la sala puede iniciar la partida.";
                }
                else
                {
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
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

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
            string username = "Anónimo";
            lock (sala)
            {
                var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                username = jugador?.Nombre ?? "Anónimo";
            }
            await Clients.Group(codigoSala).SendAsync("RecibirChatRapido", username, mensajeId);
        }

        /// <summary>
        /// Retransmite una reacción de emoji o sonido en tiempo real al grupo de la sala.
        /// </summary>
        public async Task EnviarReaccion(string codigoSala, string tipoReaccion, string valor)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala)) return;
            string username = "Anónimo";
            lock (sala)
            {
                var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                username = jugador?.Nombre ?? "Anónimo";
            }
            await Clients.Group(codigoSala).SendAsync("ReaccionRecibida", username, tipoReaccion, valor);
        }

        /// <summary>
        /// El jugador busca o crea una partida rápida para emparejarse según su nivel.
        /// </summary>
        public async Task UnirsePartidaRapida(TipoJuego juego, int nivelJugador)
        {
            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            // Determinar la apuesta en base al nivel del jugador
            TipoApuesta apuesta = TipoApuesta.Baja;
            if (nivelJugador >= 15) apuesta = TipoApuesta.Alta;
            else if (nivelJugador >= 5) apuesta = TipoApuesta.Media;

            // 1. Intentar buscar una sala pública existente que califique
            var salaExistente = SalasActivas.Values.FirstOrDefault(s => 
                s.TipoJuego == juego && 
                !s.EsPrivada && 
                s.Apuesta == apuesta && 
                s.Estado == EstadoSala.EsperandoJugadores && 
                s.Jugadores.Count < 4);

            if (salaExistente != null)
            {
                lock (salaExistente)
                {
                    if (salaExistente.Jugadores.Count < 4 && salaExistente.Estado == EstadoSala.EsperandoJugadores)
                    {
                        var nuevoJugador = new JugadorMesa
                        {
                            UsuarioId = usuarioId,
                            Nombre = username,
                            ConnectionId = Context.ConnectionId,
                            EsBot = false,
                            EsCreador = false
                        };
                        salaExistente.Jugadores.Add(nuevoJugador);
                    }
                }
                await Groups.AddToGroupAsync(Context.ConnectionId, salaExistente.CodigoSala);
                await Clients.Group(salaExistente.CodigoSala).SendAsync("SalaActualizada", salaExistente);
                
                // Si la sala se llenó a 4, iniciamos automáticamente
                if (salaExistente.Jugadores.Count == 4)
                {
                    await IniciarPartidaDesdeMatchmaking(salaExistente.CodigoSala);
                }
                return;
            }

            // 2. Si no hay sala, crear una nueva sala pública
            string codigoSala = GenerarCodigoSala();
            var nuevaSala = new Sala
            {
                CodigoSala = codigoSala,
                TipoJuego = juego,
                Apuesta = apuesta,
                Estado = EstadoSala.EsperandoJugadores,
                AcumularMas = false,
                EsPrivada = false
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

            while (!SalasActivas.TryAdd(codigoSala, nuevaSala))
            {
                codigoSala = GenerarCodigoSala();
                nuevaSala.CodigoSala = codigoSala;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, codigoSala);
            await Clients.Caller.SendAsync("SalaCreada", nuevaSala);

            // Programar un temporizador para completar con bots si no se une nadie en 5 segundos
            string codigoParaProgramar = codigoSala;
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                if (SalasActivas.TryGetValue(codigoParaProgramar, out var s) && s.Estado == EstadoSala.EsperandoJugadores)
                {
                    await IniciarPartidaMatchmakingConBots(s.CodigoSala, nivelJugador);
                }
            });
        }

        private async Task IniciarPartidaMatchmakingConBots(string codigoSala, int nivelJugador)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala) || sala.Estado != EstadoSala.EsperandoJugadores) return;

            lock (sala)
            {
                int botContador = 1;
                while (sala.Jugadores.Count < 4)
                {
                    sala.Jugadores.Add(new JugadorMesa
                    {
                        UsuarioId = null,
                        Nombre = $"Bot_Nvl{nivelJugador}_{botContador++}",
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
            }

            await Clients.Group(codigoSala).SendAsync("PartidaIniciada");
            await EnviarEstadoRedactado(sala);
            await VerificarYProcesarTurnoBot(sala);
        }

        private async Task IniciarPartidaDesdeMatchmaking(string codigoSala)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala) || sala.Estado != EstadoSala.EsperandoJugadores) return;

            lock (sala)
            {
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
            }

            await Clients.Group(codigoSala).SendAsync("PartidaIniciada");
            await EnviarEstadoRedactado(sala);
            await VerificarYProcesarTurnoBot(sala);
        }

        public async Task JugarContraBots(TipoJuego juego, int nivelJugador)
        {
            string username = Context.User?.Identity?.Name ?? $"Jugador_{Guid.NewGuid().ToString()[..4]}";
            int? usuarioId = ObtenerUsuarioIdDesdeContexto();

            string codigoSala = GenerarCodigoSala();
            
            // Determinar la apuesta en base al nivel del jugador
            TipoApuesta apuesta = TipoApuesta.Baja;
            if (nivelJugador >= 15) apuesta = TipoApuesta.Alta;
            else if (nivelJugador >= 5) apuesta = TipoApuesta.Media;

            var nuevaSala = new Sala
            {
                CodigoSala = codigoSala,
                TipoJuego = juego,
                Apuesta = apuesta,
                Estado = EstadoSala.EnProgreso,
                AcumularMas = false,
                EsPrivada = true
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

            int botContador = 1;
            while (nuevaSala.Jugadores.Count < 4)
            {
                nuevaSala.Jugadores.Add(new JugadorMesa
                {
                    UsuarioId = null,
                    Nombre = $"Bot_Nvl{nivelJugador}_{botContador++}",
                    ConnectionId = null,
                    EsBot = true,
                    EsCreador = false
                });
            }

            var nombresJugadores = nuevaSala.Jugadores.Select(j => j.Nombre).ToList();
            if (juego == TipoJuego.Uno)
            {
                nuevaSala.EstadoUno = _motorUno.InicializarJuego(nombresJugadores);
            }
            else if (juego == TipoJuego.Domino)
            {
                nuevaSala.EstadoDomino = _motorDomino.InicializarJuego(nombresJugadores);
            }

            while (!SalasActivas.TryAdd(codigoSala, nuevaSala))
            {
                codigoSala = GenerarCodigoSala();
                nuevaSala.CodigoSala = codigoSala;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, codigoSala);
            await Clients.Caller.SendAsync("SalaCreada", nuevaSala);
            await Clients.Group(codigoSala).SendAsync("PartidaIniciada");
            await EnviarEstadoRedactado(nuevaSala);
            await VerificarYProcesarTurnoBot(nuevaSala);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var acciones = new List<Func<Task>>();
            var salasARemover = new List<string>();

            // Creamos una copia estable para iterar y evitar InvalidOperationException
            var salas = SalasActivas.Values.ToList();

            foreach (var sala in salas)
            {
                lock (sala)
                {
                    var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                    if (jugador != null)
                    {
                        if (sala.Estado == EstadoSala.EsperandoJugadores)
                        {
                            sala.Jugadores.Remove(jugador);
                            string connectionId = Context.ConnectionId;
                            string codigo = sala.CodigoSala;
                            acciones.Add(async () =>
                            {
                                await Groups.RemoveFromGroupAsync(connectionId, codigo);
                                await Clients.Group(codigo).SendAsync("SalaActualizada", sala);
                            });
                        }
                        else if (sala.Estado == EstadoSala.EnProgreso)
                        {
                            jugador.EsBot = true;
                            jugador.ConnectionId = null;
                            string nombre = jugador.Nombre;
                            acciones.Add(async () =>
                            {
                                await Clients.Group(sala.CodigoSala).SendAsync("JugadorSeConvirtióEnBot", nombre);
                                await EnviarEstadoRedactado(sala);
                                await VerificarYProcesarTurnoBot(sala);
                            });
                        }
                    }

                    // Limpieza: si la sala ya no tiene humanos o está terminada, se elimina de la memoria
                    if (sala.Jugadores.All(j => j.EsBot) || sala.Estado == EstadoSala.Terminada)
                    {
                        salasARemover.Add(sala.CodigoSala);
                    }
                }
            }

            foreach (var codigo in salasARemover)
            {
                SalasActivas.TryRemove(codigo, out _);
            }

            foreach (var accion in acciones)
            {
                try
                {
                    await accion();
                }
                catch
                {
                    // Ignorar errores durante el cleanup de desconexiones
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

            string? errorMessage = null;
            CartaUno? carta = null;
            ColorUno colorActualEnviar = ColorUno.Rojo;
            bool partidaTerminada = false;
            string? ganadorNombre = null;
            string? jugadorNombre = null;

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Uno || sala.EstadoUno == null)
                {
                    errorMessage = "El juego de UNO no está en progreso.";
                }
                else
                {
                    var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
                    var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                    if (jugador == null)
                    {
                        errorMessage = "No eres parte de esta sala.";
                    }
                    else if (jugador.Nombre != jugadorActual.Nombre)
                    {
                        errorMessage = "No es tu turno.";
                    }
                    else
                    {
                        jugadorNombre = jugador.Nombre;
                        var mano = sala.EstadoUno.ManosJugadores[jugador.Nombre];
                        carta = mano.FirstOrDefault(c => c.Id == cartaId);
                        if (carta == null)
                        {
                            errorMessage = "No tienes esa carta en tu mano.";
                        }
                        else if (!_motorUno.EsJugadaValida(carta, sala.EstadoUno))
                        {
                            errorMessage = "Jugada no válida.";
                        }
                        // Restricción de apilamiento
                        else if (sala.CartasAcumuladasRobar > 0 && 
                                 carta.Valor != ValorUno.MasDos && 
                                 carta.Valor != ValorUno.MasCuatro)
                        {
                            errorMessage = "Debes responder con un +2 o +4, o robar las cartas acumuladas.";
                        }
                        else
                        {
                            mano.Remove(carta);
                            sala.EstadoUno.PozoDescarte.Add(carta);

                            // Validar que el color elegido no sea Comodin
                            var elegido = (colorElegidoPorComodin == null || colorElegidoPorComodin == ColorUno.Comodin)
                                ? ColorUno.Rojo
                                : colorElegidoPorComodin.Value;

                            sala.EstadoUno.ColorActual = (carta.Color == ColorUno.Comodin) ? elegido : carta.Color;
                            sala.EstadoUno.ValorActual = carta.Valor;
                            colorActualEnviar = sala.EstadoUno.ColorActual;

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
                                if (sala.AcumularMas)
                                {
                                    sala.CartasAcumuladasRobar += (carta.Valor == ValorUno.MasDos) ? 2 : 4;
                                    saltarSiguiente = false;
                                }
                                else
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
                            }

                            if (mano.Count == 0)
                            {
                                sala.Estado = EstadoSala.Terminada;
                                partidaTerminada = true;
                                ganadorNombre = jugadorNombre;
                                SalasActivas.TryRemove(codigoSala, out _);
                            }
                            else
                            {
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
                        }
                    }
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

            if (carta != null && jugadorNombre != null)
            {
                await Clients.Group(codigoSala).SendAsync("CartaJugadaUno", jugadorNombre, carta, colorActualEnviar);
            }

            if (partidaTerminada && ganadorNombre != null)
            {
                await ProcesarFinPartidaAsync(sala, ganadorNombre);
                await Clients.Group(codigoSala).SendAsync("PartidaTerminada", ganadorNombre);
                return;
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

            string? errorMessage = null;
            string? jugadorNombre = null;
            bool roboCarta = false;
            bool puedeJugarRobada = false;
            bool pasoRecurse = false;

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Uno || sala.EstadoUno == null)
                {
                    errorMessage = "El juego de UNO no está en progreso.";
                }
                else
                {
                    var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
                    var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                    if (jugador == null)
                    {
                        errorMessage = "No eres parte de esta sala.";
                    }
                    else if (jugador.Nombre != jugadorActual.Nombre)
                    {
                        errorMessage = "No es tu turno.";
                    }
                    else
                    {
                        jugadorNombre = jugador.Nombre;

                        if (sala.AcumularMas && sala.CartasAcumuladasRobar > 0)
                        {
                            // Robar cartas acumuladas
                            for (int i = 0; i < sala.CartasAcumuladasRobar; i++)
                            {
                                RobaCartaParaJugador(sala, jugador.Nombre);
                            }
                            sala.CartasAcumuladasRobar = 0;
                            roboCarta = true;

                            // Turno se salta automáticamente
                            int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                            sala.TurnoActualIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                            pasoRecurse = true;
                        }
                        else
                        {
                            var cartaRobada = RobaCartaParaJugador(sala, jugador.Nombre);
                            if (cartaRobada != null)
                            {
                                roboCarta = true;
                                if (_motorUno.EsJugadaValida(cartaRobada, sala.EstadoUno))
                                {
                                    puedeJugarRobada = true;
                                }
                            }

                            if (!puedeJugarRobada)
                            {
                                int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                                pasoRecurse = true;
                            }
                        }
                    }
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

            if (roboCarta && jugadorNombre != null)
            {
                await Clients.Group(codigoSala).SendAsync("CartaRobadaUno", jugadorNombre);
            }

            if (puedeJugarRobada)
            {
                await EnviarEstadoRedactado(sala);
                return;
            }

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            if (pasoRecurse)
            {
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

            string? errorMessage = null;
            FichaDomino? ficha = null;
            bool partidaTerminada = false;
            string? ganadorNombre = null;
            string? jugadorNombre = null;
            bool pasoRecurse = false;

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Domino || sala.EstadoDomino == null)
                {
                    errorMessage = "El juego de Dominó no está en progreso.";
                }
                else
                {
                    var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
                    var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                    if (jugador == null)
                    {
                        errorMessage = "No eres parte de esta sala.";
                    }
                    else if (jugador.Nombre != jugadorActual.Nombre)
                    {
                        errorMessage = "No es tu turno.";
                    }
                    else
                    {
                        jugadorNombre = jugador.Nombre;
                        var mano = sala.EstadoDomino.ManosJugadores[jugador.Nombre];
                        ficha = mano.FirstOrDefault(f => f.Id == fichaId);
                        if (ficha == null)
                        {
                            errorMessage = "No tienes esa ficha en tu mano.";
                        }
                        else if (!_motorDomino.EsJugadaValida(ficha, sala.EstadoDomino, out bool puedeIzquierda, out bool puedeDerecha))
                        {
                            errorMessage = "Jugada no válida.";
                        }
                        else if (colocarAInicios && !puedeIzquierda)
                        {
                            errorMessage = "No se puede colocar al inicio.";
                        }
                        else if (!colocarAInicios && !puedeDerecha)
                        {
                            errorMessage = "No se puede colocar al final.";
                        }
                        else
                        {
                            mano.Remove(ficha);
                            _motorDomino.RealizarJugada(ficha, colocarAInicios, sala.EstadoDomino);

                            if (mano.Count == 0)
                            {
                                sala.Estado = EstadoSala.Terminada;
                                partidaTerminada = true;
                                ganadorNombre = jugador.Nombre;
                                SalasActivas.TryRemove(codigoSala, out _);
                            }
                            else if (VerificarDominoBloqueado(sala.EstadoDomino))
                            {
                                TerminarDominoBloqueado(sala, out ganadorNombre);
                                partidaTerminada = true;
                                SalasActivas.TryRemove(codigoSala, out _);
                            }
                            else
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                                pasoRecurse = true;
                            }
                        }
                    }
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

            if (ficha != null && jugadorNombre != null)
            {
                await Clients.Group(codigoSala).SendAsync("FichaJugadaDomino", jugadorNombre, ficha, colocarAInicios);
            }

            if (partidaTerminada && ganadorNombre != null)
            {
                await ProcesarFinPartidaAsync(sala, ganadorNombre);
                await Clients.Group(codigoSala).SendAsync("PartidaTerminada", ganadorNombre);
                return;
            }

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            if (pasoRecurse)
            {
                await VerificarYProcesarTurnoBot(sala);
            }
        }

        public async Task PasarORobarTurnoDomino(string codigoSala)
        {
            if (!SalasActivas.TryGetValue(codigoSala, out var sala))
            {
                await Clients.Caller.SendAsync("Error", "La sala no existe.");
                return;
            }

            string? errorMessage = null;
            string? jugadorNombre = null;
            bool humanoRobo = false;
            FichaDomino? fichaRobada = null;
            bool puedeJugarRobada = false;
            bool puedeIzquierdaRobada = false;
            bool partidaTerminada = false;
            string? ganadorNombre = null;
            bool pasoRecurse = false;

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EnProgreso || sala.TipoJuego != TipoJuego.Domino || sala.EstadoDomino == null)
                {
                    errorMessage = "El juego de Dominó no está en progreso.";
                }
                else
                {
                    var jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
                    var jugador = sala.Jugadores.FirstOrDefault(j => j.ConnectionId == Context.ConnectionId);
                    if (jugador == null)
                    {
                        errorMessage = "No eres parte de esta sala.";
                    }
                    else if (jugador.Nombre != jugadorActual.Nombre)
                    {
                        errorMessage = "No es tu turno.";
                    }
                    else
                    {
                        jugadorNombre = jugador.Nombre;
                        var mano = sala.EstadoDomino.ManosJugadores[jugador.Nombre];

                        bool tieneJugadasValidas = mano.Any(f => _motorDomino.EsJugadaValida(f, sala.EstadoDomino, out _, out _));
                        if (tieneJugadasValidas)
                        {
                            errorMessage = "Tienes fichas válidas que puedes jugar.";
                        }
                        else
                        {
                            if (sala.EstadoDomino.Pozo.Count > 0)
                            {
                                var ficha = sala.EstadoDomino.Pozo.First();
                                sala.EstadoDomino.Pozo.Remove(ficha);
                                mano.Add(ficha);
                                humanoRobo = true;
                                fichaRobada = ficha;

                                if (_motorDomino.EsJugadaValida(ficha, sala.EstadoDomino, out puedeIzquierdaRobada, out _))
                                {
                                    puedeJugarRobada = true;
                                }
                            }

                            if (!puedeJugarRobada)
                            {
                                if (VerificarDominoBloqueado(sala.EstadoDomino))
                                {
                                    TerminarDominoBloqueado(sala, out ganadorNombre);
                                    partidaTerminada = true;
                                    SalasActivas.TryRemove(codigoSala, out _);
                                }
                                else
                                {
                                    sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                                    pasoRecurse = true;
                                }
                            }
                        }
                    }
                }
            }

            if (errorMessage != null)
            {
                await Clients.Caller.SendAsync("Error", errorMessage);
                return;
            }

            if (humanoRobo && jugadorNombre != null)
            {
                await Clients.Group(codigoSala).SendAsync("FichaRobadaDomino", jugadorNombre);
            }

            if (puedeJugarRobada)
            {
                await EnviarEstadoRedactado(sala);
                return;
            }

            if (jugadorNombre != null)
            {
                await Clients.Group(codigoSala).SendAsync("JugadorPasóTurnoDomino", jugadorNombre);
            }

            if (partidaTerminada && ganadorNombre != null)
            {
                await ProcesarFinPartidaAsync(sala, ganadorNombre);
                await Clients.Group(codigoSala).SendAsync("PartidaTerminada", ganadorNombre);
                return;
            }

            await Clients.Group(codigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            if (pasoRecurse)
            {
                await VerificarYProcesarTurnoBot(sala);
            }
        }

        #endregion

        #region Métodos de Soporte Interno

        private async Task EnviarEstadoRedactado(Sala sala)
        {
            List<JugadorMesa> jugadoresSnapshot;
            lock (sala)
            {
                jugadoresSnapshot = sala.Jugadores.ToList();
            }

            foreach (var jugador in jugadoresSnapshot)
            {
                if (jugador.EsBot || string.IsNullOrEmpty(jugador.ConnectionId)) continue;

                object estadoRedactado;
                lock (sala)
                {
                    estadoRedactado = ObtenerVistaRedactada(sala, jugador.Nombre);
                }
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
                EstadoDomino = estadoDominoRedactado,
                AcumularMas = sala.AcumularMas,
                CartasAcumuladasRobar = sala.CartasAcumuladasRobar
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

            JugadorMesa jugadorActual;
            lock (sala)
            {
                jugadorActual = sala.Jugadores[sala.TurnoActualIndex];
            }
            if (!jugadorActual.EsBot) return;

            await Task.Delay(1500);

            bool pasoRecurse = false;
            bool partidaTerminada = false;
            string? ganadorNombre = null;
            
            // Para UNO
            CartaUno? cartaJugadaUno = null;
            ColorUno colorActualUno = ColorUno.Rojo;
            bool botRoboCartaUno = false;
            bool botJugoDrawnUno = false;
            CartaUno? cartaDrawnUno = null;
            
            // Para Dominó
            FichaDomino? fichaJugadaDomino = null;
            bool colocarAIniciosDomino = false;
            bool botRoboDomino = false;
            bool botJugoDrawnDomino = false;
            FichaDomino? fichaDrawnDomino = null;
            bool botPasoDomino = false;

            lock (sala)
            {
                if (sala.Estado != EstadoSala.EnProgreso) return;
                var currentJugador = sala.Jugadores[sala.TurnoActualIndex];
                if (currentJugador.Nombre != jugadorActual.Nombre || !currentJugador.EsBot) return;

                if (sala.TipoJuego == TipoJuego.Uno && sala.EstadoUno != null)
                {
                    CartaUno? cartaParaJugar = null;
                    ColorUno? colorElegidoPorComodin = null;

                    var mano = sala.EstadoUno.ManosJugadores[jugadorActual.Nombre];

                    if (sala.AcumularMas && sala.CartasAcumuladasRobar > 0)
                    {
                        // Buscar +2 o +4 en la mano del bot
                        cartaParaJugar = mano.FirstOrDefault(c => c.Valor == ValorUno.MasDos || c.Valor == ValorUno.MasCuatro);
                        if (cartaParaJugar != null && cartaParaJugar.Color == ColorUno.Comodin)
                        {
                            colorElegidoPorComodin = mano.Where(c => c.Color != ColorUno.Comodin)
                                                         .GroupBy(c => c.Color)
                                                         .OrderByDescending(g => g.Count())
                                                         .FirstOrDefault()?.Key ?? ColorUno.Rojo;
                        }
                    }
                    else
                    {
                        var resultado = _motorUno.ProcesarTurnoBot(jugadorActual.Nombre, sala.EstadoUno);
                        cartaParaJugar = resultado.CartaJugada;
                        colorElegidoPorComodin = resultado.ColorElegidoPorComodin;
                    }

                    if (cartaParaJugar != null)
                    {
                        cartaJugadaUno = cartaParaJugar;
                        sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Remove(cartaParaJugar);
                        sala.EstadoUno.PozoDescarte.Add(cartaParaJugar);

                        var elegido = (colorElegidoPorComodin == null || colorElegidoPorComodin == ColorUno.Comodin)
                            ? ColorUno.Rojo
                            : colorElegidoPorComodin.Value;

                        sala.EstadoUno.ColorActual = (cartaParaJugar.Color == ColorUno.Comodin) 
                            ? elegido 
                            : cartaParaJugar.Color;
                        sala.EstadoUno.ValorActual = cartaParaJugar.Valor;
                        colorActualUno = sala.EstadoUno.ColorActual;

                        bool saltarSiguiente = false;
                        if (cartaParaJugar.Valor == ValorUno.Reversa)
                        {
                            sala.EstadoUno.SentidoHorario = !sala.EstadoUno.SentidoHorario;
                        }
                        else if (cartaParaJugar.Valor == ValorUno.Salto)
                        {
                            saltarSiguiente = true;
                        }
                        else if (cartaParaJugar.Valor == ValorUno.MasDos || cartaParaJugar.Valor == ValorUno.MasCuatro)
                        {
                            if (sala.AcumularMas)
                            {
                                sala.CartasAcumuladasRobar += (cartaParaJugar.Valor == ValorUno.MasDos) ? 2 : 4;
                                saltarSiguiente = false;
                            }
                            else
                            {
                                int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                int siguienteIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                                var siguienteJugador = sala.Jugadores[siguienteIndex];
                                int cantidadARobar = (cartaParaJugar.Valor == ValorUno.MasDos) ? 2 : 4;
                                for (int i = 0; i < cantidadARobar; i++)
                                {
                                    RobaCartaParaJugador(sala, siguienteJugador.Nombre);
                                }
                                saltarSiguiente = true;
                            }
                        }

                        if (sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Count == 0)
                        {
                            sala.Estado = EstadoSala.Terminada;
                            partidaTerminada = true;
                            ganadorNombre = jugadorActual.Nombre;
                            SalasActivas.TryRemove(sala.CodigoSala, out _);
                        }
                        else
                        {
                            int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                            if (saltarSiguiente)
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + 2 * pasoTurno + 4) % 4;
                            }
                            else
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                            }
                            pasoRecurse = true;
                        }
                    }
                    else
                    {
                        botRoboCartaUno = true;
                        if (sala.AcumularMas && sala.CartasAcumuladasRobar > 0)
                        {
                            for (int i = 0; i < sala.CartasAcumuladasRobar; i++)
                            {
                                RobaCartaParaJugador(sala, jugadorActual.Nombre);
                            }
                            sala.CartasAcumuladasRobar = 0;

                            int paso = sala.EstadoUno.SentidoHorario ? 1 : -1;
                            sala.TurnoActualIndex = (sala.TurnoActualIndex + paso + 4) % 4;
                            pasoRecurse = true;
                        }
                        else
                        {
                            var cartaRobada = RobaCartaParaJugador(sala, jugadorActual.Nombre);
                            if (cartaRobada != null)
                            {
                                cartaDrawnUno = cartaRobada;
                                if (_motorUno.EsJugadaValida(cartaRobada, sala.EstadoUno))
                                {
                                    botJugoDrawnUno = true;
                                    sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Remove(cartaRobada);
                                    sala.EstadoUno.PozoDescarte.Add(cartaRobada);

                                    ColorUno colorElegido = cartaRobada.Color;
                                    if (cartaRobada.Color == ColorUno.Comodin)
                                    {
                                        var manoBot = sala.EstadoUno.ManosJugadores[jugadorActual.Nombre];
                                        colorElegido = manoBot.Where(c => c.Color != ColorUno.Comodin)
                                                           .GroupBy(c => c.Color)
                                                           .OrderByDescending(g => g.Count())
                                                           .FirstOrDefault()?.Key ?? ColorUno.Rojo;
                                    }

                                    sala.EstadoUno.ColorActual = colorElegido;
                                    sala.EstadoUno.ValorActual = cartaRobada.Valor;
                                    colorActualUno = sala.EstadoUno.ColorActual;

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
                                        if (sala.AcumularMas)
                                        {
                                            sala.CartasAcumuladasRobar += (cartaRobada.Valor == ValorUno.MasDos) ? 2 : 4;
                                            saltarSiguienteDrawn = false;
                                        }
                                        else
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
                                    }

                                    if (sala.EstadoUno.ManosJugadores[jugadorActual.Nombre].Count == 0)
                                    {
                                        sala.Estado = EstadoSala.Terminada;
                                        partidaTerminada = true;
                                        ganadorNombre = jugadorActual.Nombre;
                                        SalasActivas.TryRemove(sala.CodigoSala, out _);
                                    }
                                    else
                                    {
                                        int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                        if (saltarSiguienteDrawn)
                                        {
                                            sala.TurnoActualIndex = (sala.TurnoActualIndex + 2 * pasoTurno + 4) % 4;
                                        }
                                        else
                                        {
                                            sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                                        }
                                        pasoRecurse = true;
                                    }
                                }
                                else
                                {
                                    int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                    sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                                    pasoRecurse = true;
                                }
                            }
                            else
                            {
                                int pasoTurno = sala.EstadoUno.SentidoHorario ? 1 : -1;
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + pasoTurno + 4) % 4;
                                pasoRecurse = true;
                            }
                        }
                    }
                }
                else if (sala.TipoJuego == TipoJuego.Domino && sala.EstadoDomino != null)
                {
                    var resultado = _motorDomino.ProcesarTurnoBot(jugadorActual.Nombre, sala.EstadoDomino);

                    if (resultado.FichaJugada != null)
                    {
                        fichaJugadaDomino = resultado.FichaJugada;
                        colocarAIniciosDomino = resultado.ColocarAInicios;

                        sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Remove(resultado.FichaJugada);
                        _motorDomino.RealizarJugada(resultado.FichaJugada, resultado.ColocarAInicios, sala.EstadoDomino);

                        if (sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Count == 0)
                        {
                            sala.Estado = EstadoSala.Terminada;
                            partidaTerminada = true;
                            ganadorNombre = jugadorActual.Nombre;
                            SalasActivas.TryRemove(sala.CodigoSala, out _);
                        }
                        else if (VerificarDominoBloqueado(sala.EstadoDomino))
                        {
                            TerminarDominoBloqueado(sala, out ganadorNombre);
                            partidaTerminada = true;
                            SalasActivas.TryRemove(sala.CodigoSala, out _);
                        }
                        else
                        {
                            sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                            pasoRecurse = true;
                        }
                    }
                    else
                    {
                        if (sala.EstadoDomino.Pozo.Count > 0)
                        {
                            var fichaRobada = sala.EstadoDomino.Pozo.First();
                            sala.EstadoDomino.Pozo.Remove(fichaRobada);
                            sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Add(fichaRobada);
                            botRoboDomino = true;
                            fichaDrawnDomino = fichaRobada;

                            if (_motorDomino.EsJugadaValida(fichaRobada, sala.EstadoDomino, out bool puedeIzquierda, out _))
                            {
                                botJugoDrawnDomino = true;
                                colocarAIniciosDomino = puedeIzquierda;
                                sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Remove(fichaRobada);
                                _motorDomino.RealizarJugada(fichaRobada, puedeIzquierda, sala.EstadoDomino);

                                if (sala.EstadoDomino.ManosJugadores[jugadorActual.Nombre].Count == 0)
                                {
                                    sala.Estado = EstadoSala.Terminada;
                                    partidaTerminada = true;
                                    ganadorNombre = jugadorActual.Nombre;
                                    SalasActivas.TryRemove(sala.CodigoSala, out _);
                                }
                                else if (VerificarDominoBloqueado(sala.EstadoDomino))
                                {
                                    TerminarDominoBloqueado(sala, out ganadorNombre);
                                    partidaTerminada = true;
                                    SalasActivas.TryRemove(sala.CodigoSala, out _);
                                }
                                else
                                {
                                    sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                                    pasoRecurse = true;
                                }
                            }
                            else if (VerificarDominoBloqueado(sala.EstadoDomino))
                            {
                                TerminarDominoBloqueado(sala, out ganadorNombre);
                                partidaTerminada = true;
                                SalasActivas.TryRemove(sala.CodigoSala, out _);
                            }
                            else
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                                pasoRecurse = true;
                            }
                        }
                        else
                        {
                            botPasoDomino = true;
                            if (VerificarDominoBloqueado(sala.EstadoDomino))
                            {
                                TerminarDominoBloqueado(sala, out ganadorNombre);
                                partidaTerminada = true;
                                SalasActivas.TryRemove(sala.CodigoSala, out _);
                            }
                            else
                            {
                                sala.TurnoActualIndex = (sala.TurnoActualIndex + 1) % 4;
                                pasoRecurse = true;
                            }
                        }
                    }
                }
            }

            // Notificaciones de red
            if (sala.TipoJuego == TipoJuego.Uno)
            {
                if (cartaJugadaUno != null)
                {
                    await Clients.Group(sala.CodigoSala).SendAsync("BotJugóUno", jugadorActual.Nombre, cartaJugadaUno, colorActualUno);
                }
                else if (botRoboCartaUno)
                {
                    await Clients.Group(sala.CodigoSala).SendAsync("BotRobóCarta", jugadorActual.Nombre);
                    if (botJugoDrawnUno && cartaDrawnUno != null)
                    {
                        await Clients.Group(sala.CodigoSala).SendAsync("BotJugóUno", jugadorActual.Nombre, cartaDrawnUno, colorActualUno);
                    }
                }
            }
            else if (sala.TipoJuego == TipoJuego.Domino)
            {
                if (fichaJugadaDomino != null)
                {
                    await Clients.Group(sala.CodigoSala).SendAsync("BotJugóDomino", jugadorActual.Nombre, fichaJugadaDomino, colocarAIniciosDomino);
                }
                else if (botRoboDomino)
                {
                    await Clients.Group(sala.CodigoSala).SendAsync("BotRobóDomino", jugadorActual.Nombre);
                    if (botJugoDrawnDomino && fichaDrawnDomino != null)
                    {
                        await Clients.Group(sala.CodigoSala).SendAsync("BotJugóDomino", jugadorActual.Nombre, fichaDrawnDomino, colocarAIniciosDomino);
                    }
                }
                else if (botPasoDomino)
                {
                    await Clients.Group(sala.CodigoSala).SendAsync("BotPasóTurnoDomino", jugadorActual.Nombre);
                }
            }

            if (partidaTerminada && ganadorNombre != null)
            {
                await ProcesarFinPartidaAsync(sala, ganadorNombre);
                await Clients.Group(sala.CodigoSala).SendAsync("PartidaTerminada", ganadorNombre);
                return;
            }

            await Clients.Group(sala.CodigoSala).SendAsync("TurnoActualizado", sala.TurnoActualIndex);
            await EnviarEstadoRedactado(sala);

            if (pasoRecurse)
            {
                await VerificarYProcesarTurnoBot(sala);
            }
        }

        private bool VerificarDominoBloqueado(EstadoDomino estado)
        {
            if (estado.Pozo.Count > 0) return false;

            foreach (var mano in estado.ManosJugadores.Values)
            {
                if (mano.Any(f => _motorDomino.EsJugadaValida(f, estado, out _, out _)))
                {
                    return false;
                }
            }

            return true;
        }

        private void TerminarDominoBloqueado(Sala sala, out string? ganadorNombre)
        {
            sala.Estado = EstadoSala.Terminada;

            var puntajes = sala.EstadoDomino!.ManosJugadores.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum(f => f.LadoA + f.LadoB)
            );

            var minPuntos = puntajes.Values.Min();
            var ganadores = puntajes.Where(kvp => kvp.Value == minPuntos).Select(kvp => kvp.Key).ToList();

            ganadorNombre = ganadores.FirstOrDefault();
        }

        /// <summary>
        /// Procesa la asignación de monedas, experiencia, historial y desbloqueo de logros al terminar el juego.
        /// </summary>
        private async Task ProcesarFinPartidaAsync(Sala sala, string ganadorNombre)
        {
            int valorApuesta = (int)sala.Apuesta;

            foreach (var jugador in sala.Jugadores)
            {
                if (jugador.UsuarioId == null || jugador.EsBot) continue;

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == jugador.UsuarioId.Value);
                if (usuario == null) continue;

                bool esGanador = (jugador.Nombre == ganadorNombre);
                var resultado = esGanador ? ResultadoPartida.Ganador : ResultadoPartida.Perdedor;

                // Ganador neto: +3 * Apuesta. Perdedores neto: -Apuesta.
                int monedasNetas = esGanador ? (valorApuesta * 3) : -valorApuesta;

                usuario.Monedas += monedasNetas;
                if (usuario.Monedas < 0) usuario.Monedas = 0;

                // Otorgar experiencia y subir nivel si aplica
                _experienciaService.OtorgarExperienciaPorPartida(usuario, resultado, out int expGanada);

                // Crear registro de historial de partidas
                var registro = new HistorialPartida
                {
                    UsuarioId = usuario.Id,
                    Juego = sala.TipoJuego,
                    Resultado = resultado,
                    MonedasApostadas = valorApuesta,
                    MonedasGanadasOPerdidas = monedasNetas,
                    FechaPartida = DateTime.UtcNow
                };

                _context.HistorialPartidas.Add(registro);

                // Desbloqueo de logros post-partida
                await _logroService.ProcesarLogrosPostPartidaAsync(usuario.Id);
            }

            await _context.SaveChangesAsync();
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