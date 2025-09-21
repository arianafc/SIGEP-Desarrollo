using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EmpleosTB")]
    public class Empleo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmpleo { get; set; }

        [Required, StringLength(255)]
        public string NombrePuesto { get; set; }

        [StringLength(1000)]
        public string Mensaje { get; set; }

        public int IdEstado { get; set; }

        public virtual Estado Estado { get; set; }
    }
}