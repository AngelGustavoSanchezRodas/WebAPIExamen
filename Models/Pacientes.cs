using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models
{
    public class Paciente
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
        // No usamos [DatabaseGenerated] porque el ID se genera por nuestra lógica
        public string? IdPaciente { get; set; }

        [Required]
        [Column(TypeName = "varchar(150)")]
        public string NombreCompleto { get; set; } = string.Empty;

        public string? Sintomas { get; set; }

        [Required]
        [Range(1, 5)]
        public int NivelGravedad { get; set; }

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string Estado { get; set; } = "En espera"; // Por defecto "En espera"

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string MedicoResponsable { get; set; } = string.Empty;

        // Se asignará en el controlador si la BD no lo hace automáticamente
        public DateTime FechaIngreso { get; set; } = DateTime.Now;
    }
}