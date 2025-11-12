using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EstudianteProfesorVM
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Cedula { get; set; }
        public string EstadoPractica { get; set; }
        public string EstadoUsuario { get; set; }
        public string Especialidad { get; set; }
        public bool TieneRelacionEnVacante { get; set; }
        public string EstadoVacante { get; set; }
        public int? IdPracticaVacante { get; set; }
        public string TipoMensaje { get; set; }
    }
}