using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class Vacante
    {
        public int IdVacante { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(255)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La empresa es obligatoria")]
        public int IdEmpresa { get; set; }

        [Display(Name = "Requisitos")]
        [StringLength(1000)]
        public string Requerimientos { get; set; }

        [Display(Name = "Fecha límite de aplicación")]
        [DataType(DataType.Date)]
        public DateTime? FechaMaxAplicacion { get; set; }

        [Required(ErrorMessage = "Debe haber al menos 1 cupo")]
        [Range(1, int.MaxValue, ErrorMessage = "Cupos debe ser ≥ 1")]
        public int NumCupos { get; set; }

        [Display(Name = "Fecha de cierre")]
        [DataType(DataType.Date)]
        public DateTime? FechaCierre { get; set; }

        [Required(ErrorMessage = "La especialidad es obligatoria")]
        public int IdEspecialidad { get; set; }
        public string EspecialidadNombre { get; set; }

        [Required(ErrorMessage = "La modalidad es obligatoria")]
        public int IdModalidad { get; set; }
        public string ModalidadNombre { get; set; }

        [StringLength(1000)]
        public string Descripcion { get; set; }

        [Required]
        public int IdEstado { get; set; }

        // ---- Campos para mostrar en la vista ----
        public string EmpresaNombre { get; set; }
        public string EstadoNombre { get; set; }
        public int EstudiantesPostulados { get; set; }
    }
}
