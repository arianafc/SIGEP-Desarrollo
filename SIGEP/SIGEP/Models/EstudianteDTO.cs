using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EstudianteDTO
    {
        public int IdUsuario { get; set; }
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }
        public string Telefono { get; set; }
        public int IdEspecialidad { get; set; }
        public string EspecialidadNombre { get; set; }
        public int IdEstado { get; set; }
        public string EstadoNombre { get; set; }
        public string EstadoPractica { get; set; }
        public bool EstadoAcademico { get; set; }
    }
}