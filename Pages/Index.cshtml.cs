using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // Propiedades para los ajustes de tarifas visibles en la vista
        [BindProperty]
        public decimal PrecioCarroActual { get; set; }
        [BindProperty]
        public decimal PrecioMotoActual { get; set; }

        // Método auxiliar para obtener siempre la hora exacta de Colombia
        private DateTime ObtenerHoraColombia()
        {
            DateTime utcNow = DateTime.UtcNow;
            try
            {
                TimeZoneInfo zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, zonaColombia);
            }
            catch
            {
                // Respaldo por si el contenedor Linux usa otro identificador base
                TimeZoneInfo zonaColombiaAlt = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, zonaColombiaAlt);
            }
        }

        private async Task CargarTarifasAsync()
        {
            var configCarro = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "PrecioCarro");
            var configMoto = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "PrecioMoto");

            PrecioCarroActual = configCarro != null && decimal.TryParse(configCarro.ValorTexto, out decimal pCarro) ? pCarro : 10000;
            PrecioMotoActual = configMoto != null && decimal.TryParse(configMoto.ValorTexto, out decimal pMoto) ? pMoto : 5000;
        }

        public async Task OnGetAsync()
        {
            await CargarTarifasAsync();

            DateTime hoy = DateTime.Today;
            DateTime mañana = hoy.AddDays(1);

            ListaVehiculos = await _context.Vehiculos
                .Where(v => v.HoraIngreso >= hoy && v.HoraIngreso < mañana)
                .OrderByDescending(v => v.HoraIngreso)
                .ToListAsync();

            TotalCarros = ListaVehiculos.Count(v => v.Tipo == "Carro");
            TotalMotos = ListaVehiculos.Count(v => v.Tipo == "Moto");
            TotalCaja = ListaVehiculos.Sum(v => v.ValorCobrado);
        }

        public async Task<IActionResult> OnPostRegistrarMotoAsync()
        {
            await CargarTarifasAsync();

            var registro = new Vehiculo
            {
                Tipo = "Moto",
                ValorCobrado = PrecioMotoActual, // Toma el precio configurado en ajustes
                HoraIngreso = ObtenerHoraColombia() // Hora exacta de Colombia
            };

            _context.Vehiculos.Add(registro);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRegistrarCarroAsync()
        {
            await CargarTarifasAsync();

            var registro = new Vehiculo
            {
                Tipo = "Carro",
                ValorCobrado = PrecioCarroActual, // Toma el precio configurado en ajustes
                HoraIngreso = ObtenerHoraColombia() // Hora exacta de Colombia
            };

            _context.Vehiculos.Add(registro);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // Método para guardar los nuevos precios desde la sección de ajustes
        public async Task<IActionResult> OnPostActualizarTarifasAsync(decimal precioCarro, decimal precioMoto)
        {
            var configCarro = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "PrecioCarro");
            if (configCarro != null)
            {
                configCarro.ValorTexto = precioCarro.ToString();
            }
            else
            {
                _context.Configuraciones.Add(new Configuracion { Clave = "PrecioCarro", ValorTexto = precioCarro.ToString() });
            }

            var configMoto = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "PrecioMoto");
            if (configMoto != null)
            {
                configMoto.ValorTexto = precioMoto.ToString();
            }
            else
            {
                _context.Configuraciones.Add(new Configuracion { Clave = "PrecioMoto", ValorTexto = precioMoto.ToString() });
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEnviarReporteAsync(string correoDestino)
        {
            if (string.IsNullOrEmpty(correoDestino))
            {
                return RedirectToPage();
            }

            try
            {
                DateTime hoy = DateTime.Today;
                DateTime mañana = hoy.AddDays(1);

                int totalcarrosHoy = await _context.Vehiculos
                   .CountAsync(v => v.Tipo == "Carro" && v.HoraIngreso >= hoy && v.HoraIngreso < mañana);
                int totalMotosHoy = await _context.Vehiculos
                   .CountAsync(v => v.Tipo == "Moto" && v.HoraIngreso >= hoy && v.HoraIngreso < mañana);
                decimal dineroHoy = await _context.Vehiculos
                   .Where(v => v.HoraIngreso >= hoy && v.HoraIngreso < mañana)
                   .SumAsync(v => v.ValorCobrado);

                string mensajeHtml = $@"
                  <h2>📊 Informe Diario de Parqueadero</h2>
                  <p>Resumen de la jornada de hoy:</p>
                 <ul>
                    <li>🚗 <b>Carros ingresados hoy:</b> {totalcarrosHoy}</li>
                    <li>🏍️ <b>Motos ingresadas hoy:</b> {totalMotosHoy}</li>
                    <li>💰 <b>Dinero total en caja hoy:</b> ${dineroHoy:N2}</li>
                </ul> ";

                bool seCumplieron29Dias = false;
                var config = await _context.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "UltimoEnvioMensual");
                DateTime? ultimaFechaEnvio = config?.ValorFecha;

                if (ultimaFechaEnvio == null || (hoy - ultimaFechaEnvio.Value).TotalDays >= 29)
                {
                    int totalCarrosMes = await _context.Vehiculos.CountAsync(v => v.Tipo == "Carro");
                    int totalMotosMes = await _context.Vehiculos.CountAsync(v => v.Tipo == "Moto");
                    decimal dineroMes = await _context.Vehiculos.SumAsync(v => v.ValorCobrado);

                    mensajeHtml += $@"
                <hr>
                <div style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #007bff;'>
                    <h2>📈 Informe Consolidado de Cierre de Ciclo (29 Días)</h2>
                    <p>Este informe detalla el acumulado total desde el último reinicio del sistema:</p>
                    <ul>
                        <li>🚗 <b>Total de Carros Atendidos:</b> {totalCarrosMes}</li>
                        <li>🏍️ <b>Total de Motos Atendidas:</b> {totalMotosMes}</li>
                        <li>💵 <b>Ingreso Bruto del Ciclo:</b> ${dineroMes:N2}</li>
                    </ul>
                    <p><em>Ciclo completado exitosamente. Los contadores se reiniciarán para el próximo periodo.</em></p>
                </div>";

                    seCumplieron29Dias = true;
                }

                // CONFIGURACIÓN Y ENVÍO DEL CORREO USANDO VARIABLES DE ENTORNO
                string remitente = Environment.GetEnvironmentVariable("EMAIL_USER") ?? "juandavidmoscoso123@gmail.com";
                string password = Environment.GetEnvironmentVariable("EMAIL_PASS") ?? "qauj lcol wvkm caoz";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(remitente),
                    Subject = seCumplieron29Dias ? "Reporte de Parqueadero - Diario y Cierre Mensual" : "Reporte de Parqueadero - Diario",
                    Body = mensajeHtml,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(correoDestino);
                _ = Task.Run(async () =>
               {
                   try
                   {
                       using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                       {
                           Credentials = new NetworkCredential(remitente, password),
                           EnableSsl = true
                       };
                       using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                       await smtpClient.SendMailAsync(mailMessage);

                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine($"Error al enviar el correo en segundo plano:  {ex.ToString()}");
                   }



               });
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


                }

                return RedirectToPage();






            }
            catch (Exception ex)
            {
                Console.WriteLine("Error crítico enviando correo: " + ex.Message);
            }

            return RedirectToPage();
        }
    }
}