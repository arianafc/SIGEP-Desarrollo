using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("DocumentosTB")]
    public class Documento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDocumento { get; set; }

        [Required, StringLength(255)]
        public string DocumentoNombre { get; set; } // evitar choque con tipo string "Documento"

        [Required, StringLength(100)]
        public string Tipo { get; set; }

        [Required, StringLength(500)]
        public string RutaArchivo { get; set; }

        public DateTime FechaSubida { get; set; }

        public int IdUsuario { get; set; }

        public virtual Usuario Usuario { get; set; }
    }
}