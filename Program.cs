using Microsoft.EntityFrameworkCore;
using Parquing.Models;
using System;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar servicios al contenedor (Razor Pages)
builder.Services.AddRazorPages();

// 2. Registrar el DbContext de la base de datos (Compatible con Railway y local)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
else
{
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    connectionString = $"Server={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.LocalPath.TrimStart('/')};User Id={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<ParquingDbContext>(options =>
    options.UseNpgsql(connectionString));

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

using (var scopeDb = app.Services.CreateScope())
{
    var services = scopeDb.ServiceProvider;
    var context = services.GetRequiredService<ParquingDbContext>();
    context.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<ParquingDbContext>().Database.EnsureCreated();

app.Run();