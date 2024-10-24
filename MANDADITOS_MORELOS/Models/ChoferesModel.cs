using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class ChoferesModel
    {
        [Key]
        public int PersonaID {  get; set; }
        public Disponibilidad Disponibilidad { get; set; }
    }

    public enum Disponibilidad
    {
        Disponible = 1,
        Ocupado = 2,
        FueraDeServicio = 3
    }
}
