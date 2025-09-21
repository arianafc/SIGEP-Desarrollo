using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("ComunicadosTB")]
    public class Comunicado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdComunicado { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        [Required, StringLength(2000)]
        public string Informacion { get; set; }

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [StringLength(255)]
        public string Poblacion { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaLimite { get; set; }

        public int IdUsuario { get; set; }
        public int IdEstado { get; set; }

        public virtual Usuario Usuario { get; set; }
        public virtual Estado Estado { get; set; }
    }
}