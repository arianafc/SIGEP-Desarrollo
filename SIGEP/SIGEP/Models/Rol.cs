using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("RolesTB")]
    public class Rol
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRol { get; set; }

        [Required, StringLength(2000)]
        public string Descripcion { get; set; }

        public int IdEstado { get; set; }

        public virtual Estado Estado { get; set; }
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}