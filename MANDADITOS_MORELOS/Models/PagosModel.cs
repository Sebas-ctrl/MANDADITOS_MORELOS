using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class PagosModel
    {
        [Key]
        public int ID { get; set; }
        public string? PaymentID { get; set; }
        public string? Estatus { get; set; }
        public decimal Monto { get; set; }
        public DateTime? Fecha { get; set; }
    }
}
