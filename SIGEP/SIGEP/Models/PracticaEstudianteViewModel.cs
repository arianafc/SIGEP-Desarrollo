using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public partial class PracticaEstudianteViewModel
    {
        public int IdPractica { get; set; }
        public int IdVacante { get; set; }
        public int IdUsuario { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public int IdEstado { get; set; }
        public string EstadoDescripcion { get; set; }
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }
    }
}