using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("TelefonosTB")]
    public class Telefono
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTelefono { get; set; }

        public int? IdUsuario { get; set; }
        public int? IdEmpresa { get; set; }
        public int? IdEncargado { get; set; }

        [Required, StringLength(50)]
        public string Numero { get; set; }

        public virtual Usuario Usuario { get; set; }
        public virtual Empresa Empresa { get; set; }
        public virtual Encargado Encargado { get; set; }
    }
}