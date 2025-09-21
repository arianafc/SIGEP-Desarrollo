using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("VacantesPracticasTB")]
    public class VacantePractica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVacante { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        public int IdEmpresa { get; set; }

        [StringLength(1000)]
        public string Requerimientos { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaMaxAplicacion { get; set; }

        public int? NumCupos { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaCierre { get; set; }

        // según script se migró a IdModalidad
        public int? IdModalidad { get; set; }

        [StringLength(1000)]
        public string Descripcion { get; set; }

        [StringLength(255)]
        public string Tipo { get; set; }

        public int IdEstado { get; set; }

        public virtual Empresa Empresa { get; set; }
        public virtual Estado Estado { get; set; }
        public virtual Modalidad Modalidad { get; set; }
        public virtual ICollection<EspecialidadVacante> EspecialidadesVacantes { get; set; }
    }
}
