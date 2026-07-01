using Microsoft.EntityFrameworkCore;
using JuegoClub.API.Hubs;
using JuegoClub.AccesoDatos.Contexto;
using JuegoClub.LogicaNegocios.Implementaciones;
using JuegoClub.Dominio.Entidades;

var builder = WebApplication.CreateBuilder(args);

// Registrar controladores y SignalR
builder.Services.AddSignalR();
builder.Services.AddControllers();

// Registrar DbContext en memoria
builder.Services.AddDbContext<JuegoClubContext>(options =>
    options.UseInMemoryDatabase("JuegoClubDb"));

// Registrar servicios de la aplicación
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ExperienciaService>();
builder.Services.AddScoped<LogroService>();
builder.Services.AddSingleton<MotorUnoService>();
builder.Services.AddSingleton<MotorDominoService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

var app = builder.Build();

// Inicializar base de datos con algunos datos semilla (seed) para pruebas
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<JuegoClubContext>();
    context.Database.EnsureCreated();

    if (!context.Usuarios.Any())
    {
        var mockUser = new Usuario
        {
            Id = 1,
            Username = "KeyronDev",
            Email = "keyron@juegoclub.com",
            Monedas = 10000,
            Nivel = 4,
            ExperienciaActual = 250
        };

        // Generar hash/salt para la contraseña "password123" usando AuthService
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        authService.CrearPasswordHash("password123", out byte[] hash, out byte[] salt);
        mockUser.PasswordHash = hash;
        mockUser.PasswordSalt = salt;

        context.Usuarios.Add(mockUser);

        // Sembrar algunos logros por defecto
        var logro1 = new Logro { Id = 1, Titulo = "Primera Racha", Descripcion = "Ganar 5 partidas", PuntosExperienciaRecompensa = 100 };
        var logro2 = new Logro { Id = 2, Titulo = "Jugador Experimentado", Descripcion = "Llegar al nivel 10", PuntosExperienciaRecompensa = 200 };
        context.Logros.AddRange(logro1, logro2);

        // Sembrar algunos artículos de la tienda
        var articulo1 = new Articulo { Id = 1, Nombre = "Borde Fuego", Descripcion = "Un avatar rodeado de llamas vivientes", Tipo = TipoArticulo.BordeAvatar, Precio = 500, Valor = "fire_border_url" };
        var articulo2 = new Articulo { Id = 2, Nombre = "Borde Neon", Descripcion = "Luces LED retro-futuristas", Tipo = TipoArticulo.BordeAvatar, Precio = 300, Valor = "neon_border_url" };
        var articulo3 = new Articulo { Id = 3, Nombre = "Fichas de Obsidiana", Descripcion = "Un elegante aspecto de roca volcánica oscura para Dominó", Tipo = TipoArticulo.AspectoTablero, Precio = 1200, Valor = "obsidian_skin" };
        var articulo4 = new Articulo { Id = 4, Nombre = "Fichas Cyberpunk", Descripcion = "Aspecto futurista de neón cibernético", Tipo = TipoArticulo.AspectoTablero, Precio = 1500, Valor = "cyberpunk_skin" };
        var articulo5 = new Articulo { Id = 5, Nombre = "Reacción: Risas", Descripcion = "Audio de risa corta para enviar a tus oponentes", Tipo = TipoArticulo.SonidoReaccion, Precio = 100, Valor = "laugh_sound" };
        context.Articulos.AddRange(articulo1, articulo2, articulo3, articulo4, articulo5);

        // Sembrar un historial inicial para KeyronDev
        context.HistorialPartidas.Add(new HistorialPartida
        {
            Id = 1,
            UsuarioId = 1,
            Juego = TipoJuego.Uno,
            Resultado = ResultadoPartida.Ganador,
            MonedasApostadas = 500,
            MonedasGanadasOPerdidas = 500,
            FechaPartida = DateTime.UtcNow.AddHours(-2)
        });

        context.SaveChanges();
    }
}

app.UseCors("CorsPolicy");

app.MapHub<GameHub>("/gamehub");
app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
