using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("EncargadosTB")]
    public class Encargado
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEncargado { get; set; }

        [Required, StringLength(50)]
        public string Cedula { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; }

        [Required, StringLength(50)]
        public string Apellido1 { get; set; }

        [Required, StringLength(50)]
        public string Apellido2 { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaRegistro { get; set; }

        [Required, StringLength(255)]
        public string Ocupacion { get; set; }

        [Required, StringLength(255)]
        public string LugarTrabajo { get; set; }

        public int IdEstado { get; set; }

        public virtual Estado Estado { get; set; }
        public virtual ICollection<Email> Emails { get; set; }
        public virtual ICollection<Telefono> Telefonos { get; set; }
    }
}