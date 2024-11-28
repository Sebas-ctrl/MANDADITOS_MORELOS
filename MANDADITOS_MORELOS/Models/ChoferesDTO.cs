using System.ComponentModel.DataAnnotations;

namespace MANDADITOS_MORELOS.Models
{
    public class ChoferesDTO
    {
        [Key]
        public int PersonaID { get; set; }
        public string? Nombre { get; set; }
        public string? Apellidos { get; set; }

        public string? CorreoElectronico { get; set; }
        public string? Contrasenia { get; set; }
        public string? Foto { get; set; }

        public string? RefreshToken { get; set; }
        public string? ExpoPushToken { get; set; }
        public int? Disponibilidad { get; set; }

        public int? Unidad { get; set; }
        public string? Placa { get; set; }
        public string? Marca { get; set; }
        public string? Color { get; set; }
        public int? CantidadValoraciones { get; set; }
        public float? Puntuacion { get; set; }
        public int? PedidoID { get; set; }
    }
}
