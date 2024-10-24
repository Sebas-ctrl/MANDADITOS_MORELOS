using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class UnidadesModel
    {
        [Key]
        public int ID { get; set; }
        public int Unidad { get; set; }
        public string? Placa { get; set; }
        public string Marca { get; set; }
        public string Color { get; set; }
        public int? ChoferID { get; set; }
    }
}
