
builder.Services.AddSignalR();


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



app.UseCors("CorsPolicy");

app.MapHub<GameHub>("/gamehub");