using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EncargadoDTO
    {
        public int IdEncargado { get; set; }
        public string Nombre { get; set; }   
        public string Telefono { get; set; }
        public string Parentesco { get; set; }
        public string Direccion { get; set; } 
        public string Ocupacion { get; set; }
    }
}