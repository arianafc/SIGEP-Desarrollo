using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SIGEP.Models
{
    public class BolsaEmpleoVM
    {

        public List<BolsaEmpleoModel> ListaEmpleos { get; set; }    

        public BolsaEmpleoModel NuevoEmpleo { get; set; }   = new BolsaEmpleoModel();

    }
}