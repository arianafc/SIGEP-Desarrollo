using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class DashboardVM
    {
        public int EstudiantesActivos { get; set; }

        public int EmpresasRegistradas { get; set; }

        public int EstudiantesConPracticasAsignadas { get; set; }

        public int PracticasFinalizadas { get; set; }

        public List<PracticaEstudianteViewModel> UltimasPracticasAsignadas { get; set; }

        public double PorcentajeEstudiantesConPractica { get; set; }

    }
}