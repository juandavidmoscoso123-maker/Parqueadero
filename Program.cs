using Microsoft.EntityFrameworkCore;
using Parquing.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios al contenedor (Razor Pages)
builder.Services.AddRazorPages();

// 2. Registrar el DbContext de la base de datos
builder.Services.AddDbContext<ParquingDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// (Recuerda cambiar UseSqlServer por UseNpgsql o UseMySql según el motor que uses en Railway)

var app = builder.Build();

// 3. Configurar el puerto dinámico de Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// 4. Configurar el pipeline de la aplicación (Middlewares)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();


// 5. Mapeo de rutas y archivos estáticos
app.MapStaticAssets();
app.MapRazorPages();


app.Run();