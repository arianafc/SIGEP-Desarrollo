using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("AuditoriaGlobalTB")]
    public class AuditoriaGlobal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAuditoria { get; set; }

        public int IdUsuario { get; set; }

        [Required, StringLength(255)]
        public string TablaAfectada { get; set; }

        public int IdRegistro { get; set; }

        [Required, StringLength(50)]
        public string Accion { get; set; }

        [StringLength(255)]
        public string CampoAfectado { get; set; }

        [StringLength(2000)]
        public string DatosAnteriores { get; set; }

        [StringLength(2000)]
        public string DatosNuevos { get; set; }

        // navigation
        public virtual Usuario Usuario { get; set; }
    }
}