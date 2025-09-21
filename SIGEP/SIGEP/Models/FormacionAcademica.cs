using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("FormacionAcademicaTB")]
    public class FormacionAcademica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFormacion { get; set; }

        public int IdUsuario { get; set; }

        [Required, StringLength(255)]
        public string Carrera { get; set; }

        [Required, StringLength(255)]
        public string Titulo { get; set; }

        [Column(TypeName = "date")]
        public DateTime? AnnoGraduacion { get; set; }

        public virtual Usuario Usuario { get; set; }
    }
}