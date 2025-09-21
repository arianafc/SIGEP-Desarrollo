using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("BolsaEmpleoTB")]
    public class BolsaEmpleo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmpleo { get; set; }

        [Required, StringLength(255)]
        public string Empresa { get; set; }

        [StringLength(2000)]
        public string Descripcion { get; set; }

        [StringLength(2000)]
        public string Requisitos { get; set; }

        // Según el script final: ahora se usa IdModalidad (tabla ModalidadesTB)
        public int? IdModalidad { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaPublicacion { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaLimite { get; set; }

        public int IdEstado { get; set; }

        [StringLength(255)]
        public string AreaAfin { get; set; }

        public int? IdDireccion { get; set; }

        // navigations
        public virtual Modalidad Modalidad { get; set; }
        public virtual Estado Estado { get; set; }
        public virtual Direccion Direccion { get; set; }
    }
}
