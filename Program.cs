using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(); // o Razor Pages segun uses

var app = builder.Build();

// Configurar el puerto dinámico de Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Resto de tu configuración de middlewares...
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages();

app.Run();