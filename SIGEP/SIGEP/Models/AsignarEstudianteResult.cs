using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class AsignarEstudianteResult
    {
        public int ok { get; set; }      // debe llamarse igual que la columna del SP
        public string message { get; set; }
    }
}