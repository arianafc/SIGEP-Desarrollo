using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("PracticaEstudianteTB")]
    public class PracticaEstudiante
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPractica { get; set; }

        public int IdVacante { get; set; }
        public int IdUsuario { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaAplicacion { get; set; }

        public int IdEstado { get; set; }

        public virtual VacantePractica VacantePractica { get; set; }
        public virtual Usuario Usuario { get; set; }
        public virtual Estado Estado { get; set; }
    }
}