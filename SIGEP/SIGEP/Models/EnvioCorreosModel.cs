using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EnvioCorreosModel
    {
        public List<CorreoDTO> CorreosGeneral { get; set; }

        public List<CorreoDTO> CorreosEstudiantes { get; set; }

        public List<CorreoDTO> CorreosProfesores { get; set; }

        public List<CorreoDTO> CorreosEgresados { get; set; }

    }
}