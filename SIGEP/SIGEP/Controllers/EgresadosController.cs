using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    [FiltroSesion]
    public class EgresadosController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();

        [HttpGet]
        public ActionResult ListaEgresados()
        {
            ViewBag.Especialidades = db.EspecialidadesTB
                .OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.IdEspecialidad.ToString(),
                    Text = e.Nombre
                }).ToList();

            ViewBag.Anios = db.UsuariosTB
                .Where(u => u.IdRol == 4 && u.FechaEgreso.HasValue)
                .Select(u => u.FechaEgreso.Value.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .Select(a => new SelectListItem
                {
                    Value = a.ToString(),
                    Text = a.ToString()
                }).ToList();

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerEgresados(int idEspecialidad = 0, int anio = 0)
        {
            var query = from u in db.UsuariosTB
                        join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario into jue
                        from ue in jue.DefaultIfEmpty()
                        join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
                        from esp in jesp.DefaultIfEmpty()
                        where u.IdRol == 4
                        select new
                        {
                            u.IdUsuario,
                            NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? ""),
                            Generacion = u.FechaEgreso.HasValue ? u.FechaEgreso.Value.Year.ToString() : "N/A",
                            Especialidad = esp != null ? esp.Nombre : "Sin especialidad",
                            Correo = db.EmailsTB.Where(e => e.IdUsuario == u.IdUsuario).Select(e => e.Email).FirstOrDefault(),
                            Telefono = db.TelefonosTB.Where(t => t.IdUsuario == u.IdUsuario).Select(t => t.Telefono).FirstOrDefault()
                        };

            if (idEspecialidad > 0)
                query = query.Where(x => db.UsuarioEspecialidadTB.Any(u => u.IdUsuario == x.IdUsuario && u.IdEspecialidad == idEspecialidad));

            if (anio > 0)
                query = query.Where(x => x.Generacion == anio.ToString());

            var lista = query.ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }
    }
}
