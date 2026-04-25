using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// SUSTITUYE "TuNombreDeProyecto" por el nombre exacto de la raíz de tu proyecto en la imagen
namespace TuNombreDeProyecto.Models
{
    [Table("Pacientes")] // Asegura que EF busque la tabla exacta en MySQL
    public class Paciente
    {
        [Key]
        [Column(TypeName = "varchar(20)")]
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
        public string Estado { get; set; } = "En espera";

        [Required]
        [Column(TypeName = "varchar(20)")]
        public string MedicoResponsable { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; } = DateTime.Now;
    }
}