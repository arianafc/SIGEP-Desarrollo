using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class Autenticacion
    {
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

        public List<SeccionesTB> ListaSecciones { get; set; }
        public List<EspecialidadesTB> ListaEspecialidades { get; set; }
    }
}