using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EstudianteEncargadoTB")]
    public class EstudianteEncargado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEstudianteEncargado { get; set; }

        public int IdEncargado { get; set; }
        public int IdUsuario { get; set; }

        [Required, StringLength(100)]
        public string Parentesco { get; set; }

        public int IdEstado { get; set; }

        public virtual Encargado Encargado { get; set; }
        public virtual Usuario Usuario { get; set; }
        public virtual Estado Estado { get; set; }
    }
}