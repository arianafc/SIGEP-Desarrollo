using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    [Table("ProvinciasTB")]
    public class Provincia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdProvincia { get; set; }

        [Required, StringLength(255)]
        public string Nombre { get; set; }

        public virtual ICollection<Canton> Cantones { get; set; }
    }
}