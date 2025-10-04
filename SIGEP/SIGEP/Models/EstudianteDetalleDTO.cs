using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class EstudianteDetalleDTO
    {
        public int IdUsuario { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }

        public int Edad { get; set; }

        public string Correo { get; set; }
        public string Telefono { get; set; }
        public string Especialidad { get; set; }
        public string Direccion { get; set; }
        public string EstadoPractica { get; set; }

        public string Sexo { get; set; }
        public string Seccion { get; set; }

        public List<DocumentoDTO> Documentos { get; set; }
        public List<EncargadoDTO> Encargados { get; set; }
        public List<PracticaEstudianteViewModel> Practicas { get; set; }
    }
}