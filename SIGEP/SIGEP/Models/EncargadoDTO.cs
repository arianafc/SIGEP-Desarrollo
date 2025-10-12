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
        public string LugarTrabajo { get; set; } 
        public string Ocupacion { get; set; }

        public string Correo { get; set; }

        public string Cedula { get; set; }
        public DateTime FechaRegistro { get; set; }

        public int IdEstado { get; set; }



    }
}