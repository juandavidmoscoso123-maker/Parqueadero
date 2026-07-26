using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Parquing.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parquing.Pages
{
    public class SuscripcionesModel : PageModel
    {
        private readonly ParquingDbContext _context;

        public SuscripcionesModel(ParquingDbContext context)
        {
            _context = context;
        }

        public IList<Vehiculo> ListaVehiculos { get; set; } = new List<Vehiculo>();

        public async Task OnGetAsync()
        {
            ListaVehiculos = await _context.Vehiculos.ToListAsync();
        }
    }
}