using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using SIGEP.EF;

namespace SIGEP.Web.Controllers
{
    public class AdministracionGeneralController : Controller
    {
        // Fuerza UTF-8 en todas las respuestas JSON de este controlador
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
            return base.Json(data, "application/json; charset=utf-8", Encoding.UTF8, behavior);
        }

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
                            Estado = e.Descripcion,
                            u.IdEstado
                        };

                if (!string.IsNullOrWhiteSpace(rol))
                    q = q.Where(x => x.Rol == rol);

                var data = q.OrderBy(x => x.IdEstado)  // 1 Activo, 2 Inactivo
                            .ThenBy(x => x.Nombre)
                            .ToList();

                return Json(new { data }, JsonRequestBehavior.AllowGet);
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
                    if (u == null) 
                        return Json(new { ok = false, msg = "El usuario no existe." });

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

        // ===== ESPECIALIDADES =====
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
        public JsonResult CrearEspecialidad(string nombre)
        {
            try
            {
                var nom = (nombre ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nom))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    var existente = db.EspecialidadesTB.FirstOrDefault(x => x.Nombre == nom);
                    if (existente != null)
                    {
                        if (existente.IdEstado == 2)
                            return Json(new { ok = false, msg = "Ya existe una especialidad con ese nombre, pero está INACTIVA. Actívela desde Acciones." });
                        return Json(new { ok = false, msg = "Ya existe una especialidad ACTIVA con ese nombre." });
                    }

                    db.EspecialidadesTB.Add(new EF.EspecialidadesTB
                    {
                        Nombre = nom,
                        IdEstado = 1
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
        public JsonResult EditarEspecialidad(int id, string nombre)
        {
            try
            {
                var nom = (nombre ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nom))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    var esp = db.EspecialidadesTB.FirstOrDefault(x => x.IdEspecialidad == id);
                    if (esp == null) return Json(new { ok = false, msg = "La especialidad no existe." });

                    var duplicado = db.EspecialidadesTB.FirstOrDefault(x => x.Nombre == nom && x.IdEspecialidad != id);
                    if (duplicado != null)
                    {
                        if (duplicado.IdEstado == 2)
                            return Json(new { ok = false, msg = "No se puede usar ese nombre: existe otro registro INACTIVO con el mismo nombre." });
                        return Json(new { ok = false, msg = "No se puede usar ese nombre: ya existe un registro ACTIVO igual." });
                    }

                    esp.Nombre = nom;
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
        public JsonResult CambiarEstadoEspecialidad(int id, string nuevoEstado)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    int idEstado = string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase) ? 1 :
                                   string.Equals(nuevoEstado, "Inactivo", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                    if (idEstado == 0) return Json(new { ok = false, msg = "Estado no válido." });

                    // Bloquear desactivación solo si hay usuarios ACTIVOS relacionados
                    if (idEstado == 2)
                    {
                        bool tieneUsuariosActivos = (
                            from ue in db.UsuarioEspecialidadTB
                            join u in db.UsuariosTB on ue.IdUsuario equals u.IdUsuario
                            where ue.IdEspecialidad == id && u.IdEstado == 1
                            select u.IdUsuario
                        ).Any();

                        if (tieneUsuariosActivos)
                            return Json(new { ok = false, msg = "No se puede desactivar: hay usuarios activos relacionados." });
                    }

                    var esp = db.EspecialidadesTB.FirstOrDefault(x => x.IdEspecialidad == id);
                    if (esp == null) return Json(new { ok = false, msg = "La especialidad no existe." });

                    esp.IdEstado = idEstado;
                    db.SaveChanges();
                    return Json(new { ok = true, msg = $"Especialidad {(idEstado == 1 ? "activada" : "desactivada")}." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo cambiar el estado." });
            }
        }

        // ===== SECCIONES =====
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
        public JsonResult CrearSeccion(string nombreSeccion)
        {
            try
            {
                var nom = (nombreSeccion ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nom))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    var existente = db.SeccionesTB.FirstOrDefault(x => x.Seccion == nom);
                    if (existente != null)
                    {
                        if (existente.IdEstado == 2)
                            return Json(new { ok = false, msg = "Ya existe una sección con ese nombre, pero está INACTIVA. Actívela desde Acciones." });
                        return Json(new { ok = false, msg = "Ya existe una sección ACTIVA con ese nombre." });
                    }

                    db.SeccionesTB.Add(new EF.SeccionesTB
                    {
                        Seccion = nom,
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
        public JsonResult EditarSeccion(int id, string nombreSeccion)
        {
            try
            {
                var nom = (nombreSeccion ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nom))
                    return Json(new { ok = false, msg = "El nombre es requerido." });

                using (var db = new SIGEPEntities())
                {
                    var s = db.SeccionesTB.FirstOrDefault(x => x.IdSeccion == id);
                    if (s == null) return Json(new { ok = false, msg = "La sección no existe." });

                    var duplicado = db.SeccionesTB.FirstOrDefault(x => x.Seccion == nom && x.IdSeccion != id);
                    if (duplicado != null)
                    {
                        if (duplicado.IdEstado == 2)
                            return Json(new { ok = false, msg = "No se puede usar ese nombre: existe otro registro INACTIVO con el mismo nombre." });
                        return Json(new { ok = false, msg = "No se puede usar ese nombre: ya existe un registro ACTIVO igual." });
                    }

                    s.Seccion = nom;
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
        public JsonResult CambiarEstadoSeccion(int id, string nuevoEstado)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    int idEstado = string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase) ? 1 :
                                   string.Equals(nuevoEstado, "Inactivo", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
                    if (idEstado == 0) return Json(new { ok = false, msg = "Estado no válido." });

                    if (idEstado == 2)
                    {
                        bool tieneUsuariosActivos = db.UsuariosTB.Any(u => u.IdSeccion == id && u.IdEstado == 1);
                        if (tieneUsuariosActivos)
                            return Json(new { ok = false, msg = "No se puede desactivar: hay usuarios activos relacionados." });
                    }

                    var s = db.SeccionesTB.FirstOrDefault(x => x.IdSeccion == id);
                    if (s == null) return Json(new { ok = false, msg = "La sección no existe." });

                    s.IdEstado = idEstado;
                    db.SaveChanges();
                    return Json(new { ok = true, msg = $"Sección {(idEstado == 1 ? "activada" : "desactivada")}." });
                }
            }
            catch
            {
                return Json(new { ok = false, msg = "No se pudo cambiar el estado." });
            }
        }
    }
}
