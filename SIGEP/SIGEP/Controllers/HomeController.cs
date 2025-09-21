using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

     
        [HttpGet]
        public ActionResult Registro()
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var Datos = new Autenticacion();

                    Datos.ListaEspecialidades = dbContext.EspecialidadesTB
     .Where(e => e.IdEstado == 1)
     .ToList();

                    Datos.ListaSecciones = dbContext.SeccionesTB
                        .Where(s => s.IdEstado == 1)
                        .ToList();

                    return View(Datos);
                }
            }
            catch (Exception e)
            {
                TempData["SwalError"] = "Ocurrió un error al cargar el formulario de registro. Por favor, inténtelo de nuevo más tarde.";
                return View(); 
            }
        }



        [HttpGet]

        public ActionResult Login()
        {
            ViewBag.Mensaje = Session["MensajeError"];
            Session["MensajeError"] = null;
            return View();
        }

        [HttpPost]
        public ActionResult Login(Autenticacion usuario)
        {
       

            bool credencialesValidas = false;


            if (!credencialesValidas)
            {
                TempData["SwalError"] = "Lo sentimos, el usuario no se encuentra registrado. Por favor, crea una cuenta";
                return View("Login", usuario);
            }


            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Registro(Autenticacion usuario)
        {

            TempData["SwalSuccess"] = "Usuario registrado correctamente";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Logout()
        {
            return RedirectToAction("Login");
        }


        [HttpGet]

        public ActionResult RecuperarAcceso()
        {
            return View();
        }

        [HttpPost]

        public ActionResult RecuperarAcceso(Autenticacion usuario)
        {
            var cedula = "118810955";
            if (usuario.Cedula == cedula)
            {
                TempData["SwalSuccess"] = "Hemos enviado un link de recuperación al correo ari*****@gmail.com";
                return RedirectToAction("Login");
            }
            else
            {
                TempData["SwalError"] = "La cédula proporcionada no se encuentra registrada";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CerrarSesion()
        {
            Session.Abandon();
            Session.Clear();
            return RedirectToAction("Login");
        }

    }
}