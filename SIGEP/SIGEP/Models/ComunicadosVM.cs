using Sigep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class ComunicadosVM
    {
        public List<ComunicadoCardVM> ListaComunicadosGeneral { get; set; }
        public List<ComunicadoCardVM> ListaComunicadosEstudiantes { get; set; }
        public List<ComunicadoCardVM> ListaComunicadosProfesores { get; set; }
        public List<ComunicadoCardVM> ListaComunicadosEgresados { get; set; }
        public List<ComunicadoCardVM> AllComunicados { get; set; }

    }
}