using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("ModalidadesTB")]
    public class Modalidad
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdModalidad { get; set; }

        [Required, StringLength(100)]
        public string Descripcion { get; set; }

        public virtual ICollection<VacantePractica> VacantesPracticas { get; set; }
        public virtual ICollection<BolsaEmpleo> BolsaEmpleos { get; set; }
    }
}