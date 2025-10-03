using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class DocumentoDTO
    {
        public int IdDocumento { get; set; }
        public string Documento { get; set; } 
        public string Tipo { get; set; }
        public string RutaArchivo { get; set; }
        public DateTime FechaSubida { get; set; }
        public int IdUsuario { get; set; }
    }
}