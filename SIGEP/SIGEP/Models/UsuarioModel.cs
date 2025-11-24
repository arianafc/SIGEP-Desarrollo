using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class UsuarioModel
    {
        public String NuevaContrasenna { get; set; }
        public int IdUsuario { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }
        public string Contrasenna { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaEgreso { get; set; }
        public int IdSeccion { get; set; }
        public int IdDireccion { get; set; }
        public int IdRol { get; set; }
        public int IdEstado { get; set; }

        public string Telefono { get; set; }
        public string CorreoPersonal { get; set; }
        public string CorreoMEP { get; set; }

        public int IdEspecialidad { get; set; }    

        public string NombreEspecialidad { get; set; }

        public string NombreSeccion { get; set; }
        public List<SeccionesTB> ListaSecciones { get; set; }
        public List<EspecialidadesTB> ListaEspecialidades { get; set; }
        public string Padecimiento { get; set; }

        public string Alergia { get; set; }

        public string Tratamiento { get; set; }

        public string Nacionalidad { get; set; }

        public string Sexo { get; set; }
        
        //ATRIBUTOS DIRECCION
        public string DireccionExacta { get; set; }

        public string provincia { get; set; }
        public string canton { get; set; }
        public string distrito { get; set; }




        public List<string> EmailsUsuario { get; set; }
        public List<EncargadoDTO> ListaEncargados { get; set; }

        public List<EncargadoDTO> ListaEncargadoMostrar { get; set; }


        //ATRIBUTO INFO ACADEMICA

        public string Carrera { get; set; }
        public int AnnoGraduacion { get; set; }
        public string TituloObtenido { get; set; }


        //ATRIBUTO INFO LABORAL

        public string EmpresaActual{ get; set; }

        public string PuestoActual { get; set; }


        public List<UsuarioEspecialidadModel> ListaEspecialidadesUsuario { get; set; }
    }
}