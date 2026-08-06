using System;

namespace Parquing.Models // Cambia "TuProyecto" por el nombre real del namespace de tu proyecto
{
    public class Configuracion
    {
        public int Id { get; set; }
        public required string Clave { get; set; }
        public DateTime? ValorFecha { get; set; }
        public string? ValorTexto { get; set; }
    }
}