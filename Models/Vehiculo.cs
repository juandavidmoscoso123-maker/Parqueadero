namespace Parquing.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // "Carro" o "Moto"
        public DateTime HoraIngreso { get; set; } = DateTime.Now;
        public decimal ValorCobrado { get; set; }
    }
}