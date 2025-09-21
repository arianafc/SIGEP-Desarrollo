using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EmpresasTB")]
    public class Empresa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmpresa { get; set; }

        [Required, StringLength(255)]
        public string NombreEmpresa { get; set; }

        [StringLength(255)]
        public string NombreContacto { get; set; }

        public int? IdDireccion { get; set; }

        [StringLength(255)]
        public string AreasAfines { get; set; }

        public int IdEstado { get; set; }

        public virtual Direccion Direccion { get; set; }
        public virtual Estado Estado { get; set; }
        public virtual ICollection<VacantePractica> VacantesPracticas { get; set; }
        public virtual ICollection<Email> Emails { get; set; }
        public virtual ICollection<Telefono> Telefonos { get; set; }
    }
}