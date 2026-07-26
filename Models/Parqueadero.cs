namespace Parquing.Models
{
    public class Parqueadero
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}