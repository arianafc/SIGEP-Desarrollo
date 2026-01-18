using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    [FiltroSesion] // Acceso restringido a usuarios con sesión activa
    [ValidarUsuarioActivo]
    public class VacantesController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();

        // ============================================================
        // LISTA DE VACANTES (EGRESADOS)
        // ============================================================
        [FiltroSesion]
        [FiltroEgresado] // Acceso restringido a coordinadores
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

        [HttpGet]
        public JsonResult ObtenerVacantesEgresado(string area = "", int? idModalidad = null)
        {
            try
            {
                var estadoActivo = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion.Trim().ToLower() == "activo");
                int idEstadoActivo = estadoActivo != null ? estadoActivo.IdEstado : 0;

                var query = from b in db.BolsaEmpleoTB
                            join m in db.ModalidadesTB on b.IdModalidad equals m.IdModalidad
                            join est in db.EstadosTB on b.IdEstado equals est.IdEstado
                            where b.IdEstado == idEstadoActivo
                                  && (string.IsNullOrEmpty(area) || b.AreaAfin == area)
                                  && (!idModalidad.HasValue || b.IdModalidad == idModalidad.Value)
                            orderby b.FechaPublicacion descending
                            select new
                            {
                                b.IdEmpleo,
                                Empresa = b.Empresa,
                                b.Descripcion,
                                b.Requisitos,
                                Modalidad = m.Descripcion,
                                b.AreaAfin,
                                b.FechaPublicacion,
                                b.FechaLimite,
                                b.NombrePuesto,
                                Estado = est.Descripcion
                            };

                var lista = query.ToList();

                if (!lista.Any())
                {
                    return Json(new
                    {
                        SinDatos = true,
                        Mensaje = "No hay vacantes disponibles por el momento."
                    }, JsonRequestBehavior.AllowGet);
                }

                var resultado = lista.Select(x => new
                {
                    x.IdEmpleo,
                    x.Empresa,
                    x.Descripcion,
                    x.Requisitos,
                    x.Modalidad,
                    x.AreaAfin,
                    x.NombrePuesto,
                    FechaPublicacion = x.FechaPublicacion.ToString("yyyy-MM-dd"),
                    FechaLimite = x.FechaLimite.ToString("yyyy-MM-dd"),
                    x.Estado
                });

                return Json(resultado, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Error = true,
                    Mensaje = "Error al obtener las vacantes: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
