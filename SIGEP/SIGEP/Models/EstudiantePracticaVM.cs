using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EstudiantePracticaVM
    {
        public int IdUsuario { get; set; }
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }
        public string Especialidad { get; set; }
        public bool EstadoAcademico { get; set; }
        public string EstadoPractica { get; set; }
        public string Vacante { get; set; }
        public string Empresa { get; set; }
        public string Tipo { get; set; }
    }
}