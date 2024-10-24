using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class ValoracionesModel
    {
        [Key]
        public int ID { get; set; }
        public int? Cantidad {  get; set; }
        public float Puntuacion {  get; set; }
        public int ChoferID {  get; set; }
    }
}
