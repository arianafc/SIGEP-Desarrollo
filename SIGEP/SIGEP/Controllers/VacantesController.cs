using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    [FiltroSesion] // Acceso restringido a usuarios con sesión activa
    public class VacantesController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();

        // ============================================================
        // LISTA DE VACANTES (EGRESADOS)
        // ============================================================
        [FiltroSesion]
        [FiltroCoordinador] // Acceso restringido a coordinadores
        [HttpGet]
        public ActionResult ListaVacantesEgresado()
        {
            try
            {

                // 🔹 Validar sesión
                if (Session["IdRol"] == null)
                {
                    return RedirectToAction("Index", "Home"); // No hay sesión → login
                }

                int idRol = Convert.ToInt32(Session["IdRol"]);

                // 🔹 Solo los egresados (IdRol = 4) pueden acceder
                if (idRol != 4)
                {
                    return RedirectToAction("Login", "Home"); // No autorizado → login
                }

                // Lista de áreas profesionales
                ViewBag.Areas = db.BolsaEmpleoTB
                    .Where(a => a.AreaAfin != null && a.AreaAfin != "")
                    .Select(a => a.AreaAfin)
                    .Distinct()
                    .OrderBy(a => a)
                    .Select(a => new SelectListItem
                    {
                        Text = a,
                        Value = a
                    })
                    .ToList();

                // Lista de modalidades
                ViewBag.Modalidades = db.ModalidadesTB
                    .OrderBy(m => m.Descripcion)
                    .Select(m => new SelectListItem
                    {
                        Text = m.Descripcion,
                        Value = m.IdModalidad.ToString()
                    })
                    .ToList();

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar filtros: " + ex.Message;
                return View();
            }
        }


    }
}
