using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EstudianteDetalleVM
    {
        public EstudianteDetalleDTO Estudiante { get; set; }
        public List<EncargadoDTO> Encargados { get; set; }
        public List<DocumentoDTO> Documentos { get; set; }
        public List<PracticaEstudianteViewModel> Postulaciones { get; set; }
    }
}