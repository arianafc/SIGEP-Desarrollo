using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class VacanteViewModel
    {
        public int IdVacante { get; set; }
        public string Nombre { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEstado { get; set; }
        public string Requerimientos { get; set; }
        public DateTime? FechaMaxAplicacion { get; set; }
        public int NumCupos { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int IdModalidad { get; set; }
        public string Descripcion { get; set; }

        // Campo extra para la relación many-to-many
        public int IdEspecialidad { get; set; }

        // Datos relacionados
        public string EmpresaNombre { get; set; }
        public string EspecialidadNombre { get; set; }
        public string ModalidadNombre { get; set; }
        public string EstadoNombre { get; set; }

        // Ubicación (se hereda de Empresa)
        public string Ubicacion { get; set; }
    }
}