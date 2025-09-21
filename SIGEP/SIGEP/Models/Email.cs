using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EmailsTB")]
    public class Email
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmail { get; set; }

        public int? IdUsuario { get; set; }
        public int? IdEmpresa { get; set; }
        public int? IdEncargado { get; set; }

        [Required, StringLength(255)]
        public string EmailDireccion { get; set; }

        public virtual Usuario Usuario { get; set; }
        public virtual Empresa Empresa { get; set; }
        public virtual Encargado Encargado { get; set; }
    }
}