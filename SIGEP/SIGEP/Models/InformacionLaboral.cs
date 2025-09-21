using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("InformacionLaboralTB")]
    public class InformacionLaboral
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdLaboral { get; set; }

        public int IdUsuario { get; set; }

        [StringLength(255)]
        public string EmpresaActual { get; set; }

        [StringLength(255)]
        public string PuestoActual { get; set; }

        public virtual Usuario Usuario { get; set; }
    }
}