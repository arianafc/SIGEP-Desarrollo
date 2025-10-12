//using SIGEP.EF;
//using SIGEP.Models;
//using SIGEP.Services;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Web.Mvc;

//namespace SIGEP.Controllers
//{
//    [FiltroSesion]
//    public class EgresadosController : Controller
//    {
//        private SIGEPEntities db = new SIGEPEntities();
//        Utilitarios utilitarios = new Utilitarios();

//        // ==============================
//        // VISTA PRINCIPAL
//        // ==============================
//        [HttpGet]
//        public ActionResult ListaEgresados()  // ⚠️ igual que la vista
//        {
//            ViewBag.Especialidades = ObtenerEspecialidades();
//            ViewBag.Anios = ObtenerAniosGraduacion();
//            return View();
//        }

//        //        // ==============================
//        //        // LISTADO EGRESADOS (para DataTable)
//        //        // ==============================
//        //        [HttpGet]
//        //        public JsonResult GetEgresados(int? idEstado = null, int idEspecialidad = 0)
//        //        {
//        //            var query = from u in db.UsuariosTB
//        //                        join e in db.EstadosTB on u.IdEstado equals e.IdEstado into je
//        //                        from e in je.DefaultIfEmpty()
//        //                        join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario into jue
//        //                        from ue in jue.DefaultIfEmpty()
//        //                        join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
//        //                        from esp in jesp.DefaultIfEmpty()
//        //                        where u.IdRol == 4
//        //                        select new
//        //                        {
//        //                            u.IdUsuario,
//        //                            NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? ""),
//        //                            u.Cedula,
//        //                            u.FechaEgreso,
//        //                            IdEstado = (int?)u.IdEstado,
//        //                            EstadoNombre = e != null ? e.Descripcion : "",
//        //                            IdEspecialidad = ue != null ? ue.IdEspecialidad : 0,
//        //                            EspecialidadNombre = esp != null ? esp.Nombre : ""
//        //                        };

//        //            if (idEstado.HasValue && idEstado.Value > 0)
//        //                query = query.Where(x => x.IdEstado == idEstado.Value);

//        //            if (idEspecialidad > 0)
//        //                query = query.Where(x => x.IdEspecialidad == idEspecialidad);

//        //            var lista = query.OrderByDescending(x => x.IdUsuario).ToList();

//        //            var salida = lista.Select(x => new
//        //            {
//        //                x.IdUsuario,
//        //                x.NombreCompleto,
//        //                x.Cedula,
//        //                FechaEgreso = x.FechaEgreso.HasValue ? x.FechaEgreso.Value.ToString("yyyy-MM-dd") : "",
//        //                x.IdEstado,
//        //                x.EstadoNombre,
//        //                x.IdEspecialidad,
//        //                x.EspecialidadNombre
//        //            });

//        //            return Json(new { data = salida }, JsonRequestBehavior.AllowGet);
//        //        }

//        //        // ==============================
//        //        // DETALLE EGRESADO
//        //        // ==============================
//        //        [HttpGet]
//        //        public JsonResult Detalle(int id)
//        //        {
//        //            var egresado = (from u in db.UsuariosTB
//        //                            join e in db.EstadosTB on u.IdEstado equals e.IdEstado into je
//        //                            from e in je.DefaultIfEmpty()
//        //                            join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario into jue
//        //                            from ue in jue.DefaultIfEmpty()
//        //                            join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
//        //                            from esp in jesp.DefaultIfEmpty()
//        //                            join f in db.FormacionAcademicaTB on u.IdUsuario equals f.IdUsuario into jf
//        //                            from f in jf.DefaultIfEmpty()
//        //                            join i in db.InformacionLaboralTB on u.IdUsuario equals i.IdUsuario into ji
//        //                            from i in ji.DefaultIfEmpty()
//        //                            where u.IdUsuario == id && u.IdRol == 4
//        //                            select new
//        //                            {
//        //                                u.IdUsuario,
//        //                                u.Nombre,
//        //                                u.Apellido1,
//        //                                u.Apellido2,
//        //                                u.Cedula,
//        //                                FechaEgreso = (DateTime?)u.FechaEgreso,
//        //                                Estado = e != null ? e.Descripcion : "",
//        //                                Especialidad = esp != null ? esp.Nombre : "",
//        //                                Carrera = f != null ? f.Carrera : "",
//        //                                Titulo = f != null ? f.Titulo : "",
//        //                                AnnoGraduacion = f != null ? (DateTime?)f.AnnoGraduacion : (DateTime?)null,
//        //                                EmpresaActual = i != null ? i.EmpresaActual : "",
//        //                                PuestoActual = i != null ? i.PuestoActual : ""
//        //                            }).FirstOrDefault();

//        //            if (egresado == null)
//        //                return Json(new { ok = false, message = "Egresado no encontrado" }, JsonRequestBehavior.AllowGet);

//        //            var data = new
//        //            {
//        //                egresado.IdUsuario,
//        //                NombreCompleto = $"{egresado.Nombre} {egresado.Apellido1} {egresado.Apellido2}".Trim(),
//        //                egresado.Cedula,
//        //                FechaEgreso = egresado.FechaEgreso.HasValue ? egresado.FechaEgreso.Value.ToString("yyyy-MM-dd") : "",
//        //                egresado.Estado,
//        //                egresado.Especialidad,
//        //                egresado.Carrera,
//        //                egresado.Titulo,
//        //                AnnoGraduacion = egresado.AnnoGraduacion.HasValue ? egresado.AnnoGraduacion.Value.ToString("yyyy-MM-dd") : "",
//        //                egresado.EmpresaActual,
//        //                egresado.PuestoActual
//        //            };

//        //            return Json(new { ok = true, data = data }, JsonRequestBehavior.AllowGet);
//        //        }

//        //        // ==============================
//        //        // MÉTODOS AUXILIARES
//        //        // ==============================
//        //        private List<SelectListItem> ObtenerEstados()
//        //        {
//        //            return db.EstadosTB
//        //                .OrderBy(x => x.Descripcion)
//        //                .Select(x => new SelectListItem
//        //                {
//        //                    Value = x.IdEstado.ToString(),
//        //                    Text = x.Descripcion
//        //                }).ToList();
//        //        }

//        //        private List<SelectListItem> ObtenerEspecialidades()
//        //        {
//        //            return db.EspecialidadesTB
//        //                .OrderBy(x => x.Nombre)
//        //                .Select(x => new SelectListItem
//        //                {
//        //                    Value = x.IdEspecialidad.ToString(),
//        //                    Text = x.Nombre
//        //                }).ToList();
//        //        }
//        //    }
//        //}
//        [HttpGet]
//        public JsonResult ObtenerEgresados(int idEspecialidad = 0, int anio = 0)
//        {
//            var lista = (from u in db.UsuariosTB
//                         join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario into jue
//                         from ue in jue.DefaultIfEmpty()
//                         join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
//                         from esp in jesp.DefaultIfEmpty()
//                         where u.IdRol == 4 // Solo egresados
//                               && (idEspecialidad == 0 || ue.IdEspecialidad == idEspecialidad)
//                               && (anio == 0 || (u.FechaEgreso.HasValue && u.FechaEgreso.Value.Year == anio))
//                         select new
//                         {
//                             u.IdUsuario,
//                             NombreCompleto = (u.Nombre ?? "") + " " + (u.Apellido1 ?? "") + " " + (u.Apellido2 ?? ""),
//                             Generacion = u.FechaEgreso.HasValue ? u.FechaEgreso.Value.Year.ToString() : "N/A",
//                             Especialidad = esp != null ? esp.Nombre : "Sin especialidad",
//                             u.Correo,
//                             u.Telefono
//                         }).ToList();

//            return Json(lista, JsonRequestBehavior.AllowGet);
//        }

//        private List<SelectListItem> ObtenerEspecialidades()
//        {
//            return db.EspecialidadesTB
//                     .OrderBy(x => x.Nombre)
//                     .Select(x => new SelectListItem
//                     {
//                         Value = x.IdEspecialidad.ToString(),
//                         Text = x.Nombre
//                     }).ToList();
//        }
//        private List<SelectListItem> ObtenerAniosGraduacion()
//        {
//            return db.UsuariosTB
//                     .Where(u => u.IdRol == 4 && u.FechaEgreso.HasValue)
//                     .Select(u => u.FechaEgreso.Value.Year)
//                     .Distinct()
//                     .OrderByDescending(y => y)
//                     .Select(y => new SelectListItem
//                     {
//                         Value = y.ToString(),
//                         Text = y.ToString()
//                     }).ToList();
//        }
//    }
//}

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

        // ==============================
        // VISTA PRINCIPAL
        // ==============================
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

        // ==============================
        // OBTENER LISTA DE EGRESADOS (DataTable)
        // ==============================
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
                            Nombre = u.Nombre,
                            Apellido1 = u.Apellido1,
                            Apellido2 = u.Apellido2,
                            FechaEgreso = u.FechaEgreso,
                            EspecialidadNombre = esp != null ? esp.Nombre : null
                        };

            // Aplicar filtros de forma segura (ue puede ser null)
            if (idEspecialidad > 0)
            {
                query = query.Where(x => x.EspecialidadNombre != null &&
                                         db.EspecialidadesTB.Any(e => e.IdEspecialidad == idEspecialidad && e.Nombre == x.EspecialidadNombre));
                // Nota: esta comparación usa el nombre para garantizar que el join sea respetado; 
                // si prefieres filtrar por ue.IdEspecialidad directamente, deberías proyectar ue.IdEspecialidad en la selección arriba.
            }

            if (anio > 0)
            {
                query = query.Where(x => x.FechaEgreso.HasValue && x.FechaEgreso.Value.Year == anio);
            }

            // Materializar lista
            var listaBase = query.ToList();

            // Proyectar final y traer correo/telefono desde tablas correspondientes
            var resultado = listaBase.Select(x => new
            {
                IdUsuario = x.IdUsuario,
                NombreCompleto = string.Join(" ", new[] { x.Nombre, x.Apellido1, x.Apellido2 }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Generacion = x.FechaEgreso.HasValue ? x.FechaEgreso.Value.Year.ToString() : "N/A",
                Especialidad = !string.IsNullOrEmpty(x.EspecialidadNombre) ? x.EspecialidadNombre : "Sin especialidad",
                Correo = db.EmailsTB.Where(e => e.IdUsuario == x.IdUsuario).Select(e => e.Email).FirstOrDefault(),
                Telefono = db.TelefonosTB.Where(t => t.IdUsuario == x.IdUsuario).Select(t => t.Telefono).FirstOrDefault()
            }).ToList();

            return Json(resultado, JsonRequestBehavior.AllowGet);
        }

    }
}

