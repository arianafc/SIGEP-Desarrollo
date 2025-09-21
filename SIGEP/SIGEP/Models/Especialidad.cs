using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EspecialidadesTB")]
    public class Especialidad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEspecialidad { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        public int IdEstado { get; set; }

        public virtual Estado Estado { get; set; }
        public virtual ICollection<EspecialidadVacante> EspecialidadesVacantes { get; set; }
        public virtual ICollection<UsuarioEspecialidad> UsuariosEspecialidades { get; set; }
    }
}