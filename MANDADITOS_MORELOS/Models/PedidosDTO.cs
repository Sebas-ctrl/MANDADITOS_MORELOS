using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MANDADITOS_MORELOS.Models
{
    public class PedidosDTO
    {
        [Key]
        public int ID { get; set; }
        public int? Tipo { get; set; }
        public string? LugarOrigen { get; set; }
        public string? LugarDestino { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? Estatus { get; set; }
        public PersonasModel? Cliente { get; set; }
        public PersonasModel? Chofer { get; set; }
        public PagosModel? Pago { get; set; }
    }

    public class Coordenadas
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }
}
