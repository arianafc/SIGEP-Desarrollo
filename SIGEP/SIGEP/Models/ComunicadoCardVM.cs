using System;

namespace Sigep.Models  
{
    public class ComunicadoCardVM
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public DateTime? FechaAplicacion { get; set; }
        public string Descripcion { get; set; }
        public string PublicadoPor { get; set; }
        public string DirigidoA { get; set; }
    }
}
