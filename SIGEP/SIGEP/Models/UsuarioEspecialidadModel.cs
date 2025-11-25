using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class UsuarioEspecialidadModel
    {
        public int IdEspecialidad {  get; set; }

        public string Nombre { get; set; }

        public int IdUsuario { get; set; }

        public int IdUsuarioEspecialidad { get; set; }

        public int IdEstado { get; set; }
    }
}