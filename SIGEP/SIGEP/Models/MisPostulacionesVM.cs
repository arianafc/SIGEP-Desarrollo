using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class MisPostulacionesVM
    {
        public List<PostulacionEstudianteVM> Postulaciones { get; set; } = new List<PostulacionEstudianteVM>();
        public bool EstadoAcademico { get; set; } = true;
    }

    public class PostulacionEstudianteVM
    {
        public int IdPractica { get; set; }
        public int IdVacante { get; set; }
        public int IdUsuario { get; set; }
        public string NombreVacante { get; set; }
        public string NombreEmpresa { get; set; }
        public string EstadoPractica { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public bool EsAutogestionada { get; set; }
    }

    public class AutogestionPracticaVM
    {
        public string NombreEmpresa { get; set; }
        public string Sector { get; set; }
        public string NombreEncargado { get; set; }
        public string Puesto { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Provincia { get; set; }
        public string Canton { get; set; }
        public string Distrito { get; set; }
        public string DireccionExacta { get; set; }
        public string DescripcionTareas { get; set; }
        public string Duracion { get; set; }
        public int IdModalidad { get; set; }
        public int especialidadEstudiante { get; set; }
        
    }
}