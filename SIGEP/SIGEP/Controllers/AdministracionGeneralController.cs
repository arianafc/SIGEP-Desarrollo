using System;
using System.Linq;
using System.Web.Mvc;
using SIGEP.EF;

namespace SIGEP.Web.Controllers
{
    
    public class AdministracionGeneralController : Controller
    {
        // ===== VISTA =====
        [HttpGet]
        public ActionResult AdministracionGeneral(string tab = "usuarios")
        {
            ViewBag.Tab = string.IsNullOrWhiteSpace(tab) ? "usuarios" : tab;
            return View("~/Views/AdministracionGeneral/AdministracionGeneral.cshtml");
        }

        // ===== USUARIOS =====
        [HttpGet]
        public JsonResult Usuarios(string rol = null)
        {
            using (var db = new SIGEPEntities())
            {
                var q = from u in db.UsuariosTB
                        join r in db.RolesTB on u.IdRol equals r.IdRol
                        join e in db.EstadosTB on u.IdEstado equals e.IdEstado
                        select new
                        {
                            u.IdUsuario,
                            Nombre = (u.Nombre + " " + u.Apellido1 + " " + u.Apellido2).Trim(),
                            u.Cedula,
                            Email = db.EmailsTB.Where(x => x.IdUsuario == u.IdUsuario)
                                               .Select(x => x.Email)
                                               .FirstOrDefault(),
                            Rol = r.Descripcion,
                            Estado = e.Descripcion, // "Activo"/"Inactivo"
                            u.IdEstado
                        };

                if (!string.IsNullOrWhiteSpace(rol))
                    q = q.Where(x => x.Rol == rol);

                var data = q.OrderBy(x => x.IdEstado)  // 1 -> Activo, 2 -> Inactivo
                            .ThenBy(x => x.Nombre)
                            .ToList();

                return new JsonResult
                {
                    Data = new { data },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    MaxJsonLength = int.MaxValue
                };
            }
        }

        [HttpPost]
        public JsonResult CambiarEstadoUsuario(int idUsuario, string nuevoEstado)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    int idEstado;
                    if (string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase)) idEstado = 1;
                    else if (string.Equals(nuevoEstado, "Inactivo", StringComparison.OrdinalIgnoreCase)) idEstado = 2;
                    else return Json(new { ok = false, msg = "Estado no válido." });

                    var u = db.UsuariosTB.FirstOrDefault(x => x.IdUsuario == idUsuario);
                    if (u == null) return Json(new { ok = false, msg = "El usuario no existe." });

                    u.IdEstado = idEstado;
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Estado actualizado correctamente." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "Ocurrió un error al cambiar el estado." });
            }
        }

        [HttpPost]
        public JsonResult CambiarRolUsuario(int idUsuario, string rol)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var r = db.RolesTB.FirstOrDefault(x => x.Descripcion == rol);
                    if (r == null) return Json(new { ok = false, msg = "Rol no válido." });

                    var u = db.UsuariosTB.FirstOrDefault(x => x.IdUsuario == idUsuario);
                    if (u == null) return Json(new { ok = false, msg = "El usuario no existe." });

                    u.IdRol = r.IdRol;
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Rol actualizado." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "Ocurrió un error al cambiar el rol." });
            }
        }

        // ===== ESPECIALIDADES ===== (nunca borrar; desactivar si NO hay usuarios ACTIVOS relacionados)
        [HttpGet]
        public JsonResult Especialidades()
        {
            using (var db = new SIGEPEntities())
            {
                var data = db.EspecialidadesTB
                             .Select(x => new { x.IdEspecialidad, x.Nombre, x.IdEstado })
                             .OrderBy(x => x.IdEstado)
                             .ThenBy(x => x.Nombre)
                             .ToList();

                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CrearEspecialidad(string nombre, string descripcion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    db.EspecialidadesTB.Add(new EF.EspecialidadesTB
                    {
                        Nombre = nombre.Trim(),
                        IdEstado = 1 // Activo
                    });
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Especialidad creada correctamente." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo crear la especialidad." });
            }
        }

        [HttpPost]
        public JsonResult EditarEspecialidad(int id, string nombre, string descripcion)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var esp = db.EspecialidadesTB.FirstOrDefault(x => x.IdEspecialidad == id);
                    if (esp == null) return Json(new { ok = false, msg = "La especialidad no existe." });

                    esp.Nombre = (nombre ?? "").Trim();
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Cambios guardados." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo editar la especialidad." });
            }
        }

        [HttpPost]
        public JsonResult DesactivarEspecialidad(int id)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    // Solo bloquear si hay usuarios ACTIVOS relacionados vía UsuarioEspecialidadTB
                    bool tieneUsuariosActivos = (
                        from ue in db.UsuarioEspecialidadTB
                        join u in db.UsuariosTB on ue.IdUsuario equals u.IdUsuario
                        where ue.IdEspecialidad == id && u.IdEstado == 1
                        select u.IdUsuario
                    ).Any();

                    if (tieneUsuariosActivos)
                        return Json(new { ok = false, msg = "No se puede desactivar: hay usuarios activos relacionados." });

                    var esp = db.EspecialidadesTB.FirstOrDefault(x => x.IdEspecialidad == id);
                    if (esp == null) return Json(new { ok = false, msg = "La especialidad no existe." });

                    esp.IdEstado = 2; // Inactivo
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Especialidad desactivada." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo desactivar la especialidad." });
            }
        }

        // ===== SECCIONES ===== (nunca borrar; desactivar si NO hay usuarios ACTIVOS relacionados)
        [HttpGet]
        public JsonResult Secciones()
        {
            using (var db = new SIGEPEntities())
            {
                var data = db.SeccionesTB
                             .Select(x => new { x.IdSeccion, Seccion = x.Seccion, x.IdEstado })
                             .OrderBy(x => x.IdEstado)
                             .ThenBy(x => x.Seccion)
                             .ToList();
                return Json(new { data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult CrearSeccion(string nombreSeccion, string descripcionSeccion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombreSeccion))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    db.SeccionesTB.Add(new EF.SeccionesTB
                    {
                        Seccion = nombreSeccion.Trim(),
                        IdEstado = 1
                    });
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Sección creada correctamente." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo crear la sección." });
            }
        }

        [HttpPost]
        public JsonResult EditarSeccion(int id, string nombreSeccion, string descripcionSeccion)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var s = db.SeccionesTB.FirstOrDefault(x => x.IdSeccion == id);
                    if (s == null) return Json(new { ok = false, msg = "La sección no existe." });

                    s.Seccion = (nombreSeccion ?? "").Trim();
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Cambios guardados." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo editar la sección." });
            }
        }

        [HttpPost]
        public JsonResult DesactivarSeccion(int id)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    // Bloquear solo si hay usuarios ACTIVOS con esa sección
                    bool tieneUsuariosActivos = db.UsuariosTB.Any(u => u.IdSeccion == id && u.IdEstado == 1);
                    if (tieneUsuariosActivos)
                        return Json(new { ok = false, msg = "No se puede desactivar: hay usuarios activos relacionados." });

                    var s = db.SeccionesTB.FirstOrDefault(x => x.IdSeccion == id);
                    if (s == null) return Json(new { ok = false, msg = "La sección no existe." });

                    s.IdEstado = 2; // Inactivo
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Sección desactivada." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo desactivar la sección." });
            }
        }
    }
}
