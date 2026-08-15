using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Parquing.Models;

namespace Parquing.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ParquingDbContext _context;
        private readonly HttpClient _httpClient;


        public IndexModel(
            ParquingDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        public IList<Vehiculo> ListaVehiculos { get; set; } = new List<Vehiculo>();

        public decimal TotalCaja { get; set; }
        public int TotalCarros { get; set; }
        public int TotalMotos { get; set; }

        [BindProperty]
        public decimal PrecioCarroActual { get; set; }

        [BindProperty]
        public decimal PrecioMotoActual { get; set; }

        private DateTime ObtenerHoraColombia()
        {
            DateTime utcNow = DateTime.UtcNow;

            try
            {
                TimeZoneInfo zonaColombia =
                    TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

                return TimeZoneInfo.ConvertTimeFromUtc(
                    utcNow,
                    zonaColombia
                );
            }
            catch
            {
                TimeZoneInfo zonaColombia =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "SA Pacific Standard Time"
                    );

                return TimeZoneInfo.ConvertTimeFromUtc(
                    utcNow,
                    zonaColombia
                );
            }
        }

        private async Task CargarTarifasAsync()
        {
            var configCarro =
                await _context.Configuraciones
                    .FirstOrDefaultAsync(c => c.Clave == "PrecioCarro");

            var configMoto =
                await _context.Configuraciones
                    .FirstOrDefaultAsync(c => c.Clave == "PrecioMoto");

            PrecioCarroActual =
                configCarro != null &&
                decimal.TryParse(
                    configCarro.ValorTexto,
                    out decimal pCarro
                )
                    ? pCarro
                    : 10000;

            PrecioMotoActual =
                configMoto != null &&
                decimal.TryParse(
                    configMoto.ValorTexto,
                    out decimal pMoto
                )
                    ? pMoto
                    : 5000;
        }

        public async Task OnGetAsync()
        {
            await CargarTarifasAsync();

            DateTime hoy = ObtenerHoraColombia().Date;
            DateTime manana = hoy.AddDays(1);

            ListaVehiculos = await _context.Vehiculos
                .Where(v =>
                    v.HoraIngreso >= hoy &&
                    v.HoraIngreso < manana
                )
                .OrderByDescending(v => v.HoraIngreso)
                .ToListAsync();

            TotalCarros =
                ListaVehiculos.Count(v => v.Tipo == "Carro");

            TotalMotos =
                ListaVehiculos.Count(v => v.Tipo == "Moto");

            TotalCaja =
                ListaVehiculos.Sum(v => v.ValorCobrado);
        }

        public async Task<IActionResult> OnPostRegistrarMotoAsync()
        {
            await CargarTarifasAsync();

            var registro = new Vehiculo
            {
                Tipo = "Moto",
                ValorCobrado = PrecioMotoActual,
                HoraIngreso = ObtenerHoraColombia()
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
                ValorCobrado = PrecioCarroActual,
                HoraIngreso = ObtenerHoraColombia()
            };

            _context.Vehiculos.Add(registro);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActualizarTarifasAsync(
            decimal precioCarro,
            decimal precioMoto)
        {
            var configCarro =
                await _context.Configuraciones
                    .FirstOrDefaultAsync(c => c.Clave == "PrecioCarro");

            if (configCarro != null)
            {
                configCarro.ValorTexto =
                    precioCarro.ToString();
            }
            else
            {
                _context.Configuraciones.Add(
                    new Configuracion
                    {
                        Clave = "PrecioCarro",
                        ValorTexto = precioCarro.ToString()
                    }
                );
            }

            var configMoto =
                await _context.Configuraciones
                    .FirstOrDefaultAsync(c => c.Clave == "PrecioMoto");

            if (configMoto != null)
            {
                configMoto.ValorTexto =
                    precioMoto.ToString();
            }
            else
            {
                _context.Configuraciones.Add(
                    new Configuracion
                    {
                        Clave = "PrecioMoto",
                        ValorTexto = precioMoto.ToString()
                    }
                );
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEnviarReporteAsync(
            string correoDestino)
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                return RedirectToPage();
            }

            try
            {
                DateTime hoy = ObtenerHoraColombia().Date;
                DateTime manana = hoy.AddDays(1);

                int totalCarrosHoy =
                    await _context.Vehiculos
                        .CountAsync(v =>
                            v.Tipo == "Carro" &&
                            v.HoraIngreso >= hoy &&
                            v.HoraIngreso < manana);

                int totalMotosHoy =
                    await _context.Vehiculos
                        .CountAsync(v =>
                            v.Tipo == "Moto" &&
                            v.HoraIngreso >= hoy &&
                            v.HoraIngreso < manana);

                decimal dineroHoy =
                    await _context.Vehiculos
                        .Where(v =>
                            v.HoraIngreso >= hoy &&
                            v.HoraIngreso < manana)
                        .SumAsync(v => v.ValorCobrado);

                string mensajeHtml = $@"
                <h2>📊 Informe Diario de Parqueadero</h2>

                <p>Resumen de la jornada de hoy:</p>

                <ul>
                    <li>
                        🚗 <b>Carros ingresados hoy:</b>
                        {totalCarrosHoy}
                    </li>

                    <li>
                        🏍️ <b>Motos ingresadas hoy:</b>
                        {totalMotosHoy}
                    </li>

                    <li>
                        💰 <b>Dinero total en caja hoy:</b>
                        ${dineroHoy:N2}
                    </li>
                </ul>
            ";

                bool seCumplieron29Dias = false;

                var config =
                    await _context.Configuraciones
                        .FirstOrDefaultAsync(
                            c => c.Clave == "UltimoEnvioMensual"
                        );

                DateTime? ultimaFechaEnvio =
                    config?.ValorFecha;

                if (
                    ultimaFechaEnvio == null ||
                    (hoy - ultimaFechaEnvio.Value).TotalDays >= 29
                )
                {
                    int totalCarrosMes =
                        await _context.Vehiculos
                            .CountAsync(v => v.Tipo == "Carro");

                    int totalMotosMes =
                        await _context.Vehiculos
                            .CountAsync(v => v.Tipo == "Moto");

                    decimal dineroMes =
                        await _context.Vehiculos
                            .SumAsync(v => v.ValorCobrado);

                    mensajeHtml += $@"
                    <hr>

                    <div style='
                        background-color:#f9f9f9;
                        padding:15px;
                        border-left:4px solid #007bff;
                    '>

                        <h2>
                            📈 Informe Consolidado
                            de Cierre de Ciclo (29 Días)
                        </h2>

                        <p>
                            Este informe detalla el acumulado
                            total desde el último reinicio
                            del sistema:
                        </p>

                        <ul>
                            <li>
                                🚗 <b>Total de Carros Atendidos:</b>
                                {totalCarrosMes}
                            </li>

                            <li>
                                🏍️ <b>Total de Motos Atendidas:</b>
                                {totalMotosMes}
                            </li>

                            <li>
                                💵 <b>Ingreso Bruto del Ciclo:</b>
                                ${dineroMes:N2}
                            </li>
                        </ul>

                        <p>
                            <em>
                                Ciclo completado exitosamente.
                                Los contadores se reiniciarán
                                para el próximo periodo.
                            </em>
                        </p>

                    </div>
                ";

                    seCumplieron29Dias = true;
                }

                string apiKey =
                    Environment.GetEnvironmentVariable(
                        "RESEND_API_KEY"
                    ) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Console.WriteLine(
                        "ERROR: No existe la variable de entorno RESEND_API_KEY."
                    );

                    return RedirectToPage();
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        apiKey
                    );

                var payload = new
                {
                    from = "onboarding@resend.dev",

                    to = new[]
                    {
                    correoDestino
                },

                    subject = seCumplieron29Dias
                        ? "Reporte de Parqueadero - 29 Días"
                        : "Reporte de Parqueadero",

                    html = mensajeHtml
                };

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "https://api.resend.com/emails",
                        payload
                    );

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse =
                        await response.Content.ReadAsStringAsync();

                    Console.WriteLine(
                        $"Error de Resend: {errorResponse}"
                    );

                    return RedirectToPage();
                }

                Console.WriteLine(
                    $"¡Correo enviado con éxito por Resend a {correoDestino}!"
                );

                if (seCumplieron29Dias)
                {
                    var todosLosVehiculos =
                        await _context.Vehiculos
                            .ToListAsync();

                    _context.Vehiculos.RemoveRange(
                        todosLosVehiculos
                    );

                    if (config != null)
                    {
                        config.ValorFecha = hoy;
                    }
                    else
                    {
                        _context.Configuraciones.Add(
                            new Configuracion
                            {
                                Clave = "UltimoEnvioMensual",
                                ValorFecha = hoy
                            }
                        );
                    }

                    await _context.SaveChangesAsync();
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Error enviando reporte: {ex.Message}"
                );

                return RedirectToPage();
            }
        }
    }


}
