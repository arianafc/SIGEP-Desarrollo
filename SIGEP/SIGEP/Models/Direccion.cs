using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("DireccionesTB")]
    public class Direccion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDireccion { get; set; }

        [Required, StringLength(2000)]
        public string DireccionExacta { get; set; }

        public int IdEstado { get; set; }

        // columna agregada IdDistrito
        public int IdDistrito { get; set; }

        public virtual Estado Estado { get; set; }
        public virtual Distrito Distrito { get; set; }
        public virtual ICollection<Empresa> Empresas { get; set; }
        public virtual ICollection<BolsaEmpleo> BolsaEmpleos { get; set; }
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }
}