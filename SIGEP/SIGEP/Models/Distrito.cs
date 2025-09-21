using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("DistritosTB")]
    public class Distrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDistrito { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        public int IdCanton { get; set; }

        public virtual Canton Canton { get; set; }
        public virtual ICollection<Direccion> Direcciones { get; set; }
    }
}