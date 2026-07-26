using Microsoft.EntityFrameworkCore;
using Parquing.Models;

namespace Parquing.Models
{
    public class ParquingDbContext : DbContext
    {
        public ParquingDbContext(DbContextOptions<ParquingDbContext> options) : base(options) { }

        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Configuracion> Configuraciones { get; set; }
         public DbSet<Parqueadero> Parqueaderos { get; set; }
    }
}