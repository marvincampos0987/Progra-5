using JuegoClub.API.Hubs;
using JuegoClub.LogicaNegocios.Implementaciones;

var builder = WebApplication.CreateBuilder(args);

// Registrar los motores del juego
builder.Services.AddSignalR();
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

app.UseCors("CorsPolicy");

app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "Hello World!");

app.Run();
