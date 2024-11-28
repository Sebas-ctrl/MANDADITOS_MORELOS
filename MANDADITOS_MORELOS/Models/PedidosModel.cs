using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MANDADITOS_MORELOS.Models
{
    public class PedidosModel
    {
        [Key]
        public int ID { get; set; }
        public Pedido? Tipo { get; set; }
        public string? LugarOrigen { get; set; }
        public string? LugarDestino { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public TipoEstado? Estatus { get; set; }
        public int? ClienteID { get; set; }
        public int? ChoferID { get; set; }
        public int? PagoID { get; set; }
        public bool? Puntuado { get; set; }
    }
    public enum Pedido
    {
        Envio = 1,
        Recibo = 2
    }

    public enum TipoEstado
    {
        Pendiente = 1,
        EnCurso = 2,
        Completado = 3,
        Cancelado = 4
    }
}
