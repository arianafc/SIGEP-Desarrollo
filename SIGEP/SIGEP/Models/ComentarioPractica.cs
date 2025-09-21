using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("ComentariosPracticaTB")]
    public class ComentarioPractica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdComentario { get; set; }

        [Required, StringLength(1000)]
        public string Comentario { get; set; }

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        public int IdUsuario { get; set; }

        public int IdPractica { get; set; }

        [StringLength(100)]
        public string Nota { get; set; }

        [StringLength(50)]
        public string Tipo { get; set; }

        public virtual Usuario Usuario { get; set; }
        public virtual PracticaEstudiante PracticaEstudiante { get; set; }
    }
}