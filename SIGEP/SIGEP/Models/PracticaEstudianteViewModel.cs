using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public partial class PracticaEstudianteViewModel
    {
        public int IdPractica { get; set; }          // PK en PracticaEstudianteTB
        public int IdVacante { get; set; }           // FK a VacantesTB
        public int IdUsuario { get; set; }           // FK a UsuariosTB
        public DateTime FechaAplicacion { get; set; }
        public int IdEstado { get; set; }            // FK a EstadosTB

        // ---- Info extendida para la vista ----
        public string EstadoDescripcion { get; set; }   // Nombre del estado (ej: "En curso")
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }

        public string Empresa { get; set; }             // Empresa asociada a la vacante
        public string Estado { get; set; }              // Estado textual corto
        public int IdPostulacion { get; set; }
    }
}