
using Microsoft.EntityFrameworkCore;
using Parquing.Models;
using System;

AppContext.SetSwitch(
    "Npgsql.EnableLegacyTimestampBehavior",
    true
);

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// 1. SERVICIOS
// ============================================================

builder.Services.AddRazorPages();

// HttpClient para enviar correos mediante Resend
builder.Services.AddHttpClient();


// ============================================================
// 2. CONEXIÓN A POSTGRESQL
// ============================================================

var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    connectionString =
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        );
}
else
{
    var databaseUri = new Uri(connectionString);

    var userInfo =
        databaseUri.UserInfo.Split(':');

    connectionString =
        $"Server={databaseUri.Host};" +
        $"Port={databaseUri.Port};" +
        $"Database={databaseUri.LocalPath.TrimStart('/')};" +
        $"User Id={userInfo[0]};" +
        $"Password={userInfo[1]};" +
        $"SSL Mode=Require;" +
        $"Trust Server Certificate=true;";
}


// Registrar DbContext
builder.Services.AddDbContext<ParquingDbContext>(
    options =>
        options.UseNpgsql(connectionString)
);


// ============================================================
// 3. CONSTRUIR APLICACIÓN
// ============================================================

var app = builder.Build();


// ============================================================
// 4. PUERTO DE RAILWAY
// ============================================================

var port =
    Environment.GetEnvironmentVariable("PORT")
    ?? "8080";

app.Urls.Add(
    $"http://0.0.0.0:{port}"
);


// ============================================================
// 5. MANEJO DE ERRORES
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}


// ============================================================
// 6. MIGRACIONES DE BASE DE DATOS
// ============================================================

using (var scopeDb = app.Services.CreateScope())
{
    var services = scopeDb.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<ParquingDbContext>();

        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            "Error al aplicar migración: " +
            ex.Message
        );
    }
}


// ============================================================
// 7. MIDDLEWARES
// ============================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();


// ============================================================
// 8. INICIAR APLICACIÓN
// ============================================================

app.Run();

