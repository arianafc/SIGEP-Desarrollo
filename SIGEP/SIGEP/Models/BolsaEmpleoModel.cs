using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class BolsaEmpleoModel
    {
        public int IdEmpleo { get; set; }

        public string Empresa { get; set; }

        public string Descripcion { get; set; }

        public string Requisitos { get; set; }

        public DateTime FechaPublicacion { get; set; }

        public DateTime FechaLimite { get; set; }

        public int IdEstado { get; set; }

        public string AreaAfin { get; set; }

        public string Canton { get; set; }

        public string Provincia { get; set; }

        public string Distrito { get; set; }

        public string DireccionExacta { get; set; }

        public int IdDireccion { get; set; }

        public int IdModalidad { get; set; }

        public string Modalidad { get; set; }



    }
}