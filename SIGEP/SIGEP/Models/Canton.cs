using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("CantonesTB")]
    public class Canton
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCanton { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        public int IdProvincia { get; set; }

        public virtual Provincia Provincia { get; set; }
        public virtual ICollection<Distrito> Distritos { get; set; }
    }

}