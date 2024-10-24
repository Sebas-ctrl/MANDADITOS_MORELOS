using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MANDADITOS_MORELOS.Models
{
    public class PedidosInsert
    {
        [Key]
        public int ID { get; set; }
        public Pedido Tipo { get; set; }
        public float LatitudOrigen { get; set; }
        public float LongitudOrigen { get; set; }
        public float LatitudDestino { get; set; }
        public float LongitudDestino { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public TipoEstado? Estatus { get; set; }
        public int ClienteID { get; set; }
        public int ChoferID { get; set; }
        public int PagoID { get; set; }
    }
}
