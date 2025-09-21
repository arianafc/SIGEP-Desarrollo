using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Linq;
using System.Web;
using static System.Collections.Specialized.BitVector32;

namespace SIGEP.Models
{
    [Table("EstadosTB")]
    public class Estado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEstado { get; set; }

        [Required, StringLength(255)]
        public string Descripcion { get; set; }

        public virtual ICollection<Usuario> Usuarios { get; set; }
        public virtual ICollection<Seccion> Secciones { get; set; }
        public virtual ICollection<Rol> Roles { get; set; }
    }
}