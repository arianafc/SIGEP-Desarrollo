using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class VacantePracticaVM
    {
        public int IdVacante { get; set; }
        public int IdPractica { get; set; }

        public string Nombre { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEstado { get; set; }
        public string Requerimientos { get; set; }
        public DateTime? FechaMaxAplicacion { get; set; }
        public int NumCupos { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int IdModalidad { get; set; }
        public string Descripcion { get; set; }
        public string Tipo { get; set; }
        public List<EstadoVM> ListaEstados { get; set; } = new List<EstadoVM>();

        // Campo extra para la relación many-to-many
        public int IdEspecialidad { get; set; }

        public string EmpresaNombre { get; set; }
        public string EspecialidadNombre { get; set; }
        public string ModalidadNombre { get; set; }
        public string EstadoNombre { get; set; }

        // Información del Estudiante (Usuario)
        public int IdUsuario { get; set; }
        public string EstudianteNombre { get; set; }
        public string EstudianteCedula { get; set; }
        public string EstudianteCorreo { get; set; }
        public int? EstudianteEdad { get; set; }
        public string EstudianteEspecialidad { get; set; }

        // Información de Contacto de la Empresa
        public string ContactoEmpresaNombre { get; set; }
        public string ContactoEmpresaEmail { get; set; }
        public string ContactoEmpresaTelefono { get; set; }

        // Información de la Postulación/Práctica
        public DateTime? FechaAplicacion { get; set; }
        public string EstadoPractica { get; set; }

        public string EstadoDescripcion { get; set; }
        public string UltimoComentario { get; set; }
        public DateTime? FechaUltimoComentario { get; set; }

        public List<ComentarioVM> Comentarios { get; set; } = new List<ComentarioVM>();
    }

    public class ComentarioVM
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Comentario { get; set; }
    }
}
