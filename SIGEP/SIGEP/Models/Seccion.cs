using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("SeccionesTB")]
    public class Seccion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSeccion { get; set; }

        [Required, StringLength(255)]
        public string SeccionNombre { get; set; }

        public int IdEstado { get; set; }

        public virtual Estado Estado { get; set; }
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}