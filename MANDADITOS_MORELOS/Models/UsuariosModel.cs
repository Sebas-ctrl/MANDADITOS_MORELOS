using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class UsuariosModel
    {
        [Key]
        public int PersonaID { get; set; }
        public string? Nombre { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Contrasenia { get; set; }

    }
}
