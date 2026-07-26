using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using Parquing.Models;

namespace Parquing.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ParquingDbContext _context;

    public IndexModel(ParquingDbContext context)
    {
     _context = context;
    }

        public IList<Vehiculo> ListaVehiculos { get; set; } = new List<Vehiculo>();
        public decimal TotalCaja { get; set; }
        public int TotalCarros { get; set; }
        public int TotalMotos { get; set; }

        public async Task OnGetAsync()
       {
         DateTime hoy = DateTime.Today;

         ListaVehiculos = await _context.Vehiculos
         .Where(v => v.HoraIngreso.Date == hoy)
         .OrderByDescending(v => v.HoraIngreso)
         .ToListAsync();
 
         TotalCarros = ListaVehiculos.Count(v => v.Tipo == "Carro");
         TotalMotos = ListaVehiculos.Count(v => v.Tipo == "Moto");
         TotalCaja = ListaVehiculos.Sum(v => v.ValorCobrado);
        }

        public IActionResult OnPostRegistrarMoto()
        {
            var registro = new Vehiculo
            {
                Tipo = "Moto",
                ValorCobrado = 5000,
                HoraIngreso = System.DateTime.Now
            };

            _context.Vehiculos.Add(registro);
            _context.SaveChanges();

            return RedirectToPage();
        }

        public IActionResult OnPostRegistrarCarro()
        {
            var registro = new Vehiculo
            {
                Tipo = "Carro",
                ValorCobrado = 10000,
                HoraIngreso = System.DateTime.Now
            };

            _context.Vehiculos.Add(registro);
            _context.SaveChanges();

            return RedirectToPage();
        }

   // Este método se activa automáticamente al presionar "Guardar y Enviar" en el modal
 public async Task<IActionResult> OnPostEnviarReporteAsync(string correoDestino)
{
    if (string.IsNullOrEmpty(correoDestino))
    {
        return RedirectToPage();
    }

    try
    {
        // Fecha actual
        DateTime hoy = DateTime.Today;

        // 1. REPORTE DIARIO (Vehículos registrados hoy - NO borra nada, al otro día queda limpio solo para la vista de hoy)
        int totalCarrosHoy = await _context.Vehiculos.CountAsync(v => v.Tipo == "Carro" && v. HoraIngreso .Date == hoy);
        int totalMotosHoy = await _context.Vehiculos.CountAsync(v => v.Tipo == "Moto" && v. HoraIngreso .Date == hoy);
        decimal dineroHoy = await _context.Vehiculos.Where(v => v. HoraIngreso .Date == hoy).SumAsync(v => v.ValorCobrado);

        // Diseñamos el mensaje base con el reporte diario
        string mensajeHtml = $@"
            <h2>📊 Informe Diario de Parqueadero</h2>
            <p>Resumen de la jornada de hoy:</p>
            <ul>
                <li>🚗 <b>Carros ingresados hoy:</b> {totalCarrosHoy}</li>
                <li>🏍️ <b>Motos ingresadas hoy:</b> {totalMotosHoy}</li>
                <li>💰 <b>Dinero total en caja hoy:</b> ${dineroHoy:N2}</li>
            </ul>
        ";

        bool seCumplieron29Dias = false;

        // Verificamos en la base de datos la última vez que se hizo el cierre del ciclo mensual
        var config = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "UltimoEnvioMensual");
        DateTime? ultimaFechaEnvio = config?.ValorFecha;

        // 2. REPORTE MENSUAL MEJORADO (Solo aparece y se procesa cuando se cumplan los 29 días o más)
        if (ultimaFechaEnvio == null || (hoy - ultimaFechaEnvio.Value).TotalDays >= 29)
        {
            int totalCarrosMes = await _context.Vehiculos.CountAsync(v => v.Tipo == "Carro");
            int totalMotosMes = await _context.Vehiculos.CountAsync(v => v.Tipo == "Moto");
            decimal dineroMes = await _context.Vehiculos.SumAsync(v => v.ValorCobrado);

            // Mensaje mensual mejorado y detallado del cierre de ciclo
            mensajeHtml += $@"
                <hr>
                <div style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #007bff;'>
                    <h2>📈 Informe Consolidado de Cierre de Ciclo (29 Días)</h2>
                    <p>Este informe detalla el acumulado total desde el último reinicio del sistema:</p>
                    <ul>
                        <li>🚗 <b>Total de Carros Atendidos:</b> {totalCarrosMes}</li>
                        <li>🏍️ <b>Total de Motos Atendidas:</b> {totalMotosMes}</li>
                        <li>💵 <b>Promedio / Ingreso Bruto del Ciclo:</b> ${dineroMes:N2}</li>
                    </ul>
                    <p><em>Ciclo completado exitosamente. Los contadores se reiniciarán para el próximo periodo.</em></p>
                </div>
            ";

            seCumplieron29Dias = true;
        }

        // 3. CONFIGURACIÓN Y ENVÍO DEL CORREO
        var mailMessage = new MailMessage
        {
            From = new MailAddress("juandavidmoscoso123@gmail.com"),
            Subject = seCumplieron29Dias ? "Reporte de Parqueadero - Diario y Cierre Mensual" : "Reporte de Parqueadero - Diario",
            Body = mensajeHtml,
            IsBodyHtml = true
        };

        mailMessage.To.Add(correoDestino);

        using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
        {
            smtpClient.Credentials = new NetworkCredential("juandavidmoscoso123@gmail.com", "xbod tseu tgjn qexs");
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mailMessage);
        }

        // 4. REINICIO DE DATOS: Solo se borra el historial acumulado cuando se cumplen los 29 días.
        // Los días normales el reporte diario se manda solo y los datos siguen intactos en la BD para el acumulado.
        if (seCumplieron29Dias)
        {
            var todosLosVehiculos = await _context.Vehiculos.ToListAsync();
            _context.Vehiculos.RemoveRange(todosLosVehiculos);

            if (config != null)
            {
                config.ValorFecha = hoy;
            }
            else
            {
                _context.Configuraciones.Add(new Configuracion { Clave = "UltimoEnvioMensual", ValorFecha = hoy });
            }

            await _context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }

    return RedirectToPage();
}
    
} 
} 