using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class PerfilController : Controller
    {
        [HttpGet]
        [FiltroSesion]
        public ActionResult MiPerfil()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CambiarContrasenna(Autenticacion usuario)
        {
           try
            {
                if (usuario.Contrasenna == usuario.NuevaContrasenna)
                {
                    using (var dbContext = new SIGEPEntities()) { 
                       var cedula = Session["Cedula"].ToString();
                       var result = dbContext.CambiarContrasennaSP(cedula, usuario.Contrasenna);
                        if (result != 0)
                        {
                            TempData["SwalSuccess"] = "Contraseña actualizada exitosamente.";
                        } else
                        {
                            TempData["SwalError"] = "No se pudo cambiar la contraseña, intente de nuevo.";
                        }

                    }
                } else
                {
                            TempData["SwalError"] = "La nueva contraseña no coincide con la confirmación. Intente de nuevo.";
                }
            } catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }


            return RedirectToAction("MiPerfil");
        }
    }
}