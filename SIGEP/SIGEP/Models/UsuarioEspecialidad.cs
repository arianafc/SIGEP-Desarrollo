using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("UsuarioEspecialidadTB")]
    public class UsuarioEspecialidad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuarioEspecialidad { get; set; }

        public int IdEspecialidad { get; set; }
        public int IdUsuario { get; set; }
        public int IdEstado { get; set; }

        public virtual Especialidad Especialidad { get; set; }
        public virtual Usuario Usuario { get; set; }
        public virtual Estado Estado { get; set; }
    }
}