using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EspecialidadesVacantesTB")]
    public class EspecialidadVacante
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEspecialidadVacante { get; set; }

        public int IdEspecialidad { get; set; }
        public int IdVacante { get; set; }

        public virtual Especialidad Especialidad { get; set; }
        public virtual VacantePractica VacantePractica { get; set; }
    }
}