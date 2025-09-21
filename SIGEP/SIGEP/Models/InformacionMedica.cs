using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("InformacionMedicaTB")]
    public class InformacionMedica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInfoMedica { get; set; }

        public int IdUsuario { get; set; }

        [StringLength(500)]
        public string Padecimiento { get; set; }

        [StringLength(500)]
        public string Tratamiento { get; set; }

        [StringLength(500)]
        public string Alergia { get; set; }

        public virtual Usuario Usuario { get; set; }
    }
}