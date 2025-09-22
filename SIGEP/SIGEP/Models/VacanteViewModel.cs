using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class VacanteViewModel
    {
        public int IdVacante { get; set; }
        public string Titulo { get; set; }
        public int IdEstado { get; set; }
        public string EstadoDescripcion { get; set; }
    }
}