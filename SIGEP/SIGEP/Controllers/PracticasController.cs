using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;


namespace SIGEP.Controllers
{
    //[FiltroProfesor]
    public class PracticasController : Controller
    {
        // ==============================
        // VISTA PRINCIPAL VACANTES
        // ==============================
        [HttpGet]
        //[FiltroSesion]
        //[FiltroUsuarioAdmin]
        public ActionResult VacantesEstudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Modalidades = ObtenerModalidades();
            ViewBag.Empresas = ObtenerEmpresas();
            ViewBag.Estados = ObtenerEstados();

            return View();
        }

        // ==============================
        // LISTADO VACANTES (JSON para DataTable)
        // ==============================
        [HttpGet]
        public JsonResult GetVacantes(string estado = "", int idEspecialidad = 0, int idModalidad = 0)
        {
            using (var db = new SIGEPEntities())
            {
                var query = from v in db.VacantesPracticasTB
                            join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()
                            join d in db.DireccionesTB on e.IdDireccion equals d.IdDireccion into jd
                            from d in jd.DefaultIfEmpty()
                            join es in db.EstadosTB on v.IdEstado equals es.IdEstado into jes
                            from es in jes.DefaultIfEmpty()
                            join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante into jev
                            from ev in jev.DefaultIfEmpty()
                            join sp in db.EspecialidadesTB on ev.IdEspecialidad equals sp.IdEspecialidad into jsp
                            from sp in jsp.DefaultIfEmpty()
                            join m in db.ModalidadesTB on v.IdModalidad equals m.IdModalidad into jm
                            from m in jm.DefaultIfEmpty()
                            select new VacantePracticaDTO
                            {
                                IdVacante = v.IdVacante,
                                Nombre = v.Nombre,
                                IdEmpresa = v.IdEmpresa,
                                EmpresaNombre = e != null ? e.NombreEmpresa : "",
                                Requerimientos = v.Requerimientos,
                                FechaMaxAplicacion = v.FechaMaxAplicacion,
                                NumCupos = v.NumCupos ?? 0,
                                FechaCierre = v.FechaCierre,
                                IdModalidad = v.IdModalidad ?? 0,
                                ModalidadNombre = m != null ? m.Descripcion : "",
                                Descripcion = v.Descripcion,
                                IdEspecialidad = ev != null ? ev.IdEspecialidad : 0,
                                EspecialidadNombre = sp != null ? sp.Nombre : "",
                                IdEstado = v.IdEstado,
                                EstadoNombre = es != null ? es.Descripcion : "",
                                EstudiantesPostulados = db.PracticaEstudianteTB.Count(p => p.IdVacante == v.IdVacante)
                            };

                //Excluir vacantes Inactivas
                query = query.Where(x => x.EstadoNombre != "Inactivo");

                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(x => x.EstadoNombre == estado);

                if (idEspecialidad > 0)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidad);

                if (idModalidad > 0)
                    query = query.Where(x => x.IdModalidad == idModalidad);

                var list = query.OrderByDescending(x => x.IdVacante).ToList();

                var outList = list.Select(x => new
                {
                    x.IdVacante,
                    x.Nombre,
                    x.IdEmpresa,
                    x.EmpresaNombre,
                    x.Requerimientos,
                    FechaMaxAplicacion = x.FechaMaxAplicacion.HasValue ? x.FechaMaxAplicacion.Value.ToString("o") : null,
                    x.NumCupos,
                    FechaCierre = x.FechaCierre.HasValue ? x.FechaCierre.Value.ToString("o") : null,
                    x.IdModalidad,
                    x.ModalidadNombre,
                    x.Descripcion,
                    x.IdEspecialidad,
                    x.EspecialidadNombre,
                    x.IdEstado,
                    x.EstadoNombre,
                    x.EstudiantesPostulados
                }).ToList();

                return Json(new { data = outList }, JsonRequestBehavior.AllowGet);
            }
        }
        // ==============================
        // CREAR VACANTE (POST)
        // ==============================
        [HttpPost]
        public JsonResult Crear(VacanteViewModel model)
        {
            if (model == null ||
                string.IsNullOrWhiteSpace(model.Nombre) ||
                model.IdEmpresa <= 0 ||
                string.IsNullOrWhiteSpace(model.Requerimientos) ||
                model.NumCupos < 1)
            {
                return Json(new { ok = false, message = "Debe completar todos los campos obligatorios (Nombre, Empresa, Requisitos, Cupos >= 1)" });
            }

            // 🔹 Validación de fechas
            if (model.FechaMaxAplicacion.HasValue && model.FechaCierre.HasValue)
            {
                if (model.FechaMaxAplicacion.Value > model.FechaCierre.Value)
                {
                    return Json(new { ok = false, message = "La fecha de aplicación no puede ser mayor a la fecha de cierre." });
                }
            }

            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var idEstado = model.IdEstado > 0 ? model.IdEstado : 1; // fallback
                    var vacante = new VacantesPracticasTB
                    {
                        Nombre = model.Nombre,
                        IdEmpresa = model.IdEmpresa,
                        IdEstado = idEstado,
                        Requerimientos = model.Requerimientos,
                        FechaMaxAplicacion = model.FechaMaxAplicacion,
                        NumCupos = model.NumCupos,
                        FechaCierre = model.FechaCierre,
                        IdModalidad = model.IdModalidad,
                        Descripcion = model.Descripcion
                    };

                    db.VacantesPracticasTB.Add(vacante);
                    db.SaveChanges();

                    if (model.IdEspecialidad > 0)
                    {
                        db.EspecialidadesVacantesTB.Add(new EspecialidadesVacantesTB
                        {
                            IdVacante = vacante.IdVacante,
                            IdEspecialidad = model.IdEspecialidad
                        });
                        db.SaveChanges();
                    }

                    tx.Commit();
                    return Json(new { ok = true, message = "Vacante creada correctamente" });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new { ok = false, message = "Error al crear vacante: " + ex.Message });
                }
            }
        }


        // ==============================
        // DETALLE VACANTE (GET)
        // ==============================
        public JsonResult Detalle(int id)
        {
            using (var db = new SIGEPEntities())
            {
                var d = (from v in db.VacantesPracticasTB
                         join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                         from e in je.DefaultIfEmpty()

                         join dir in db.DireccionesTB on e.IdDireccion equals dir.IdDireccion into jd
                         from dir in jd.DefaultIfEmpty()

                         join es in db.EstadosTB on v.IdEstado equals es.IdEstado into jes
                         from es in jes.DefaultIfEmpty()

                         join m in db.ModalidadesTB on v.IdModalidad equals m.IdModalidad into jm
                         from m in jm.DefaultIfEmpty()

                         join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante into jev
                         from ev in jev.DefaultIfEmpty()

                         join esp in db.EspecialidadesTB on ev.IdEspecialidad equals esp.IdEspecialidad into jesp
                         from esp in jesp.DefaultIfEmpty()

                         where v.IdVacante == id
                         select new
                         {
                             v.IdVacante,
                             v.Nombre,
                             IdEmpresa = (int?)v.IdEmpresa,
                             EmpresaNombre = e != null ? e.NombreEmpresa : "",
                             NombreContacto = e != null ? e.NombreContacto : "",
                             v.Requerimientos,
                             v.FechaMaxAplicacion,
                             v.NumCupos,
                             v.FechaCierre,
                             v.Descripcion,
                             IdModalidad = (int?)v.IdModalidad,
                             ModalidadNombre = m != null ? m.Descripcion : "",
                             IdEspecialidad = (int?)ev.IdEspecialidad,
                             EspecialidadNombre = esp != null ? esp.Nombre : "",
                             IdEstado = (int?)v.IdEstado,
                             EstadoNombre = es != null ? es.Descripcion : "",
                             Ubicacion = dir != null ? dir.DireccionExacta : "",
                             EmpresaIdForContact = e != null ? (int?)e.IdEmpresa : null
                         })
                         .AsEnumerable() // 🔹 de aquí en adelante ya es LINQ to Objects
                         .Select(x => new
                         {
                             x.IdVacante,
                             x.Nombre,
                             IdEmpresa = x.IdEmpresa ?? 0,
                             x.EmpresaNombre,
                             x.NombreContacto,
                             x.Requerimientos,
                             FechaMaxAplicacion = x.FechaMaxAplicacion.HasValue
                                 ? x.FechaMaxAplicacion.Value.ToString("yyyy-MM-dd")
                                 : null,
                             x.NumCupos,
                             FechaCierre = x.FechaCierre.HasValue
                                 ? x.FechaCierre.Value.ToString("yyyy-MM-dd")
                                 : null,
                             x.Descripcion,
                             IdModalidad = x.IdModalidad ?? 0,
                             x.ModalidadNombre,
                             IdEspecialidad = x.IdEspecialidad ?? 0,
                             x.EspecialidadNombre,
                             IdEstado = x.IdEstado ?? 0,
                             x.EstadoNombre,
                             x.Ubicacion,

                             // 🔹 Ahora sí, en memoria puedo armar las listas
                             Emails = x.EmpresaIdForContact.HasValue
                                 ? db.EmailsTB.Where(em => em.IdEmpresa == x.EmpresaIdForContact.Value)
                                              .Select(em => em.Email)
                                              .ToList()
                                 : new List<string>(),

                             Telefonos = x.EmpresaIdForContact.HasValue
                                 ? db.TelefonosTB.Where(t => t.IdEmpresa == x.EmpresaIdForContact.Value)
                                                 .Select(t => t.Telefono)
                                                 .ToList()
                                 : new List<string>()
                         })
                         .FirstOrDefault();

                return Json(new { ok = d != null, data = d }, JsonRequestBehavior.AllowGet);
            }
        }



        // ==============================
        // EDITAR VACANTE (POST)
        // ==============================
        [HttpPost]
        public JsonResult Editar(VacanteViewModel model)
        {
            if (model == null || model.IdVacante <= 0 ||
                string.IsNullOrWhiteSpace(model.Nombre) ||
                model.IdEmpresa <= 0 ||
                string.IsNullOrWhiteSpace(model.Requerimientos) ||
                model.NumCupos < 1)
            {
                return Json(new { ok = false, message = "Debe completar todos los campos obligatorios (Nombre, Empresa, Requisitos, Cupos >= 1)" });
            }

            // 🔹 Validación de fechas
            if (model.FechaMaxAplicacion.HasValue && model.FechaCierre.HasValue)
            {
                if (model.FechaMaxAplicacion.Value > model.FechaCierre.Value)
                {
                    return Json(new { ok = false, message = "La fecha de aplicación no puede ser mayor a la fecha de cierre." });
                }
            }

            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == model.IdVacante);
                    if (vacante == null)
                        return Json(new { ok = false, message = "Vacante no encontrada" });

                    vacante.Nombre = model.Nombre;
                    vacante.IdEmpresa = model.IdEmpresa;
                    vacante.Requerimientos = model.Requerimientos;
                    vacante.FechaMaxAplicacion = model.FechaMaxAplicacion;
                    vacante.NumCupos = model.NumCupos;
                    vacante.FechaCierre = model.FechaCierre;
                    vacante.IdModalidad = model.IdModalidad;
                    vacante.Descripcion = model.Descripcion;

                    db.SaveChanges();

                    // Reemplazar especialidad
                    var existentes = db.EspecialidadesVacantesTB.Where(x => x.IdVacante == vacante.IdVacante).ToList();
                    if (existentes.Any())
                        db.EspecialidadesVacantesTB.RemoveRange(existentes);

                    if (model.IdEspecialidad > 0)
                    {
                        db.EspecialidadesVacantesTB.Add(new EspecialidadesVacantesTB
                        {
                            IdVacante = vacante.IdVacante,
                            IdEspecialidad = model.IdEspecialidad
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();

                    return Json(new { ok = true, message = "Práctica actualizada correctamente" });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new { ok = false, message = "Error al actualizar vacante: " + ex.Message });
                }
            }
        }



        // ==============================
        // ELIMINAR VACANTE (POST)
        // ==============================
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == id);
                    if (vacante == null)
                    {
                        return Json(new { ok = false, message = "La vacante no existe." });
                    }

                    // Validar si tiene estudiantes asignados en PracticaEstudianteTB
                    bool tieneAsignados = db.PracticaEstudianteTB.Any(pe => pe.IdVacante == id);
                    if (tieneAsignados)
                    {
                        return Json(new { ok = false, message = "No se puede eliminar: vacante tiene estudiantes asignados." });
                    }

                    // Buscar estado "Inactivo"
                    var estadoInactivo = db.EstadosTB.FirstOrDefault(e => e.Descripcion == "Inactivo");
                    if (estadoInactivo == null)
                    {
                        return Json(new { ok = false, message = "No existe el estado 'Inactivo' en la tabla EstadosTB." });
                    }

                    // Guardar estado anterior para auditoría
                    var estadoAnterior = vacante.IdEstado;

                    // Cambiar a estado inactivo
                    vacante.IdEstado = estadoInactivo.IdEstado;
                    db.SaveChanges();

                    // Validar sesión antes de registrar auditoría
                    var idUsuarioSesion = Session["IdUsuario"] as int?;
                    if (idUsuarioSesion != null)
                    {
                        db.AuditoriaGlobalTB.Add(new AuditoriaGlobalTB
                        {
                            IdUsuario = idUsuarioSesion.Value,
                            TablaAfectada = "VacantesPracticasTB",
                            IdRegistro = vacante.IdVacante,
                            Accion = "Eliminar (Inactivar)",
                            CampoAfectado = "IdEstado",
                            DatosAnteriores = estadoAnterior.ToString(),
                            DatosNuevos = estadoInactivo.IdEstado.ToString()
                        });

                        db.SaveChanges();
                    }

                    return Json(new { ok = true, message = "Vacante eliminada (marcada como Inactivo) correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al eliminar: " + ex.Message });
            }
        }




        // ==============================
        // OBTENER POSTULACIONES
        // ==============================
        [HttpGet]
        public JsonResult ObtenerPostulaciones(int idVacante)
        {
            using (var db = new SIGEPEntities())
            {
                var lista = (from p in db.PracticaEstudianteTB
                             join u in db.UsuariosTB on p.IdUsuario equals u.IdUsuario
                             join e in db.EstadosTB on p.IdEstado equals e.IdEstado
                             where p.IdVacante == idVacante
                             orderby u.Nombre
                             select new PracticaEstudianteViewModel
                             {
                                 IdUsuario = u.IdUsuario,
                                 Cedula = u.Cedula,
                                 NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                 IdEstado = p.IdEstado,
                                 EstadoDescripcion = e.Descripcion
                             }).ToList();

                return Json(new { ok = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // OBTENER ESTUDIANTES DISPONIBLES PARA ASIGNAR (AJAX usado por modal asignar)
        // ==============================
        //[HttpGet]
        //public JsonResult ObtenerEstudiantesAsignar(int idVacante)
        //{
        //    try
        //    {
        //        using (var db = new SIGEPEntities())
        //        {
        //            // 1️⃣ Profesor logueado
        //            if (Session["IdUsuario"] == null)
        //            {
        //                return Json(new { ok = false, mensaje = "La sesión expiró o no hay usuario logueado." }, JsonRequestBehavior.AllowGet);
        //            }

        //            int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

        //            // 2️⃣ Especialidades del profesor
        //            var especialidadesProfesor = db.UsuarioEspecialidadTB
        //                .Where(ue => ue.IdUsuario == idProfesor && ue.IdEstado == 1)
        //                .Select(ue => ue.IdEspecialidad)
        //                .ToList();

        //            if (!especialidadesProfesor.Any())
        //            {
        //                return Json(new { ok = false, mensaje = "El profesor no tiene especialidades registradas." }, JsonRequestBehavior.AllowGet);
        //            }

        //            // 3️⃣ Estudiantes que coincidan con las especialidades del profesor
        //            var estudiantes = (from u in db.UsuariosTB
        //                               join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
        //                               join e in db.EspecialidadesTB on ue.IdEspecialidad equals e.IdEspecialidad
        //                               where u.IdRol == 1 // Solo estudiantes
        //                               && ue.IdEstado == 1
        //                               && especialidadesProfesor.Contains(ue.IdEspecialidad)
        //                               select new
        //                               {
        //                                   u.IdUsuario,
        //                                   NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
        //                                   u.Cedula,
        //                                   Especialidad = e.Nombre,
        //                                   EstadoPractica = db.PracticaEstudianteTB
        //                                        .Any(pe => pe.IdUsuario == u.IdUsuario)
        //                                        ? "Práctica Asignada"
        //                                        : "Sin Práctica Asignada"
        //                               }).ToList();

        //            return Json(new { ok = true, data = estudiantes }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { ok = false, mensaje = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        ////}
        //[HttpGet]
        //public JsonResult ObtenerEstudiantesAsignar(int idVacante)
        //{
        //    try
        //    {
        //        using (var db = new SIGEPEntities())
        //        {
        //            // 1️⃣ Verificar sesión
        //            if (Session["IdUsuario"] == null)
        //            {
        //                return Json(new { ok = false, mensaje = "La sesión expiró o no hay usuario logueado." }, JsonRequestBehavior.AllowGet);
        //            }

        //            int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

        //            // 2️⃣ Obtener la vacante
        //            var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == idVacante);
        //            if (vacante == null)
        //            {
        //                return Json(new { ok = false, mensaje = "La vacante no existe." }, JsonRequestBehavior.AllowGet);
        //            }

        //            // 3️⃣ Especialidad requerida de la vacante
        //            var idEspecialidadVacante = vacante.IdEspecialidad;

        //            if (idEspecialidadVacante == null)
        //            {
        //                return Json(new { ok = false, mensaje = "La vacante no tiene especialidad asociada." }, JsonRequestBehavior.AllowGet);
        //            }

        //            // 4️⃣ Validar que el profesor tenga esa especialidad
        //            bool profesorCoincide = db.UsuarioEspecialidadTB
        //                .Any(ue => ue.IdUsuario == idProfesor
        //                       && ue.IdEspecialidad == idEspecialidadVacante
        //                       && ue.IdEstado == 1);

        //            if (!profesorCoincide)
        //            {
        //                return Json(new { ok = false, mensaje = "La especialidad de la vacante no coincide con las del profesor." }, JsonRequestBehavior.AllowGet);
        //            }

        //            // 5️⃣ Obtener estudiantes de esa especialidad
        //            var estudiantes = (from u in db.UsuariosTB
        //                               join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
        //                               join e in db.EspecialidadesTB on ue.IdEspecialidad equals e.IdEspecialidad
        //                               where u.IdRol == 1 // Estudiantes
        //                               && ue.IdEstado == 1
        //                               && ue.IdEspecialidad == idEspecialidadVacante
        //                               select new
        //                               {
        //                                   u.IdUsuario,
        //                                   Cedula = u.Cedula,
        //                                   NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
        //                                   Especialidad = e.Nombre,
        //                                   EstadoPractica = db.PracticaEstudianteTB
        //                                        .Any(pe => pe.IdUsuario == u.IdUsuario && pe.IdVacante == idVacante)
        //                                        ? "Práctica Asignada"
        //                                        : "Sin Práctica Asignada"
        //                               }).Distinct().ToList();

        //            return Json(new { ok = true, data = estudiantes }, JsonRequestBehavior.AllowGet);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { ok = false, mensaje = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}



        //Tomar el IdVacante que llega al método.Buscar las especialidades ligadas a esa vacante(EspecialidadesVacantesTB).
        //Buscar estudiantes(UsuariosTB.IdRol = 1) que tengan esas mismas especialidades(UsuarioEspecialidadTB).
        //Excluir los que ya tengan una práctica asignada(PracticaEstudianteTB).

        [HttpGet]
        public JsonResult ObtenerEstudiantesAsignar(int idVacante)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    // 1️⃣ Especialidades ligadas a la vacante
                    var especialidadesVacante = db.EspecialidadesVacantesTB
                        .Where(ev => ev.IdVacante == idVacante)
                        .Select(ev => ev.IdEspecialidad)
                        .ToList();

                    if (!especialidadesVacante.Any())
                    {
                        return Json(new { ok = false, mensaje = "La vacante no tiene especialidades registradas." }, JsonRequestBehavior.AllowGet);
                    }

                    // 2️⃣ Estudiantes que coinciden con esas especialidades
                    var estudiantes = (from u in db.UsuariosTB
                                       join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
                                       join e in db.EspecialidadesTB on ue.IdEspecialidad equals e.IdEspecialidad
                                       where u.IdRol == 1 // Solo estudiantes
                                       && ue.IdEstado == 1
                                       && especialidadesVacante.Contains(ue.IdEspecialidad)
                                       select new
                                       {
                                           u.IdUsuario,
                                           NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                           u.Cedula,
                                           Especialidad = e.Nombre,
                                           EstadoPractica = db.PracticaEstudianteTB
                                                .Any(pe => pe.IdUsuario == u.IdUsuario && pe.IdVacante == idVacante)
                                                ? "Práctica Asignada"
                                                : "Sin Práctica Asignada"
                                       }).Distinct().ToList();

                    return Json(new { ok = true, data = estudiantes }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }




        // ==============================
        // ASIGNAR ESTUDIANTE A VACANTE 
        // ==============================
        [HttpPost]
        public JsonResult AsignarEstudiante(int idVacante, int idUsuario)
        {
            using (var db = new SIGEPEntities())
            {
                var estadoAsignado = db.EstadosTB.FirstOrDefault(e => e.Descripcion == "Asignado" || e.Descripcion == "Asignada");
                if (estadoAsignado == null)
                    return Json(new { ok = false, message = "El estado 'Asignado' no existe en EstadosTB" }, JsonRequestBehavior.AllowGet);

                var existente = db.PracticaEstudianteTB.FirstOrDefault(p => p.IdVacante == idVacante && p.IdUsuario == idUsuario);
                if (existente != null)
                {
                    existente.IdEstado = estadoAsignado.IdEstado;
                    existente.FechaAplicacion = DateTime.Now;
                }
                else
                {
                    db.PracticaEstudianteTB.Add(new PracticaEstudianteTB
                    {
                        IdVacante = idVacante,
                        IdUsuario = idUsuario,
                        IdEstado = estadoAsignado.IdEstado,
                        FechaAplicacion = DateTime.Now
                    });
                }

                db.SaveChanges();
                return Json(new { ok = true, message = "Estudiante asignado correctamente." }, JsonRequestBehavior.AllowGet);
            }
        }


        // ==============================
        // OBTENER ESTUDIANTES ASIGNADOS
        // ==============================
        [HttpGet]
        public JsonResult GetEstudiantesAsignados(int idVacante)
        {
            using (var db = new SIGEPEntities())
            {
                var estadoAsignado = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion == "Asignado" || e.Descripcion == "Asignada");

                if (estadoAsignado == null)
                    return Json(new { ok = false, mensaje = "No existe estado 'Asignado' en la BD" }, JsonRequestBehavior.AllowGet);

                var asignados = (from p in db.PracticaEstudianteTB
                                 join u in db.UsuariosTB on p.IdUsuario equals u.IdUsuario
                                 where p.IdVacante == idVacante && p.IdEstado == estadoAsignado.IdEstado
                                 select new
                                 {
                                     u.IdUsuario,
                                     NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                     Cedula = u.Cedula
                                 }).ToList();

                return Json(new { ok = true, data = asignados }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // DESASIGNAR ESTUDIANTE
        // ==============================
        [HttpPost]
        public JsonResult DesasignarEstudiante(int idUsuario, int idVacante)
        {
            using (var db = new SIGEPEntities())
            {
                var practica = db.PracticaEstudianteTB
                    .FirstOrDefault(p => p.IdUsuario == idUsuario && p.IdVacante == idVacante);

                if (practica == null)
                {
                    return Json(new { ok = false, mensaje = "No se encontró la práctica del estudiante." }, JsonRequestBehavior.AllowGet);
                }

                var estadoSinPractica = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion == "Sin práctica asignada");

                if (estadoSinPractica == null)
                {
                    return Json(new { ok = false, mensaje = "No existe el estado 'Sin práctica asignada' en la BD." }, JsonRequestBehavior.AllowGet);
                }

                practica.IdEstado = estadoSinPractica.IdEstado;
                db.SaveChanges();

                return Json(new { ok = true, mensaje = "Estudiante desasignado correctamente." }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // MÉTODOS PRIVADOS PARA DROPDOWNS
        // ==============================
        private List<SelectListItem> ObtenerEspecialidades()
        {
            using (var db = new SIGEPEntities())
            {
                return db.EspecialidadesTB
                         .OrderBy(e => e.Nombre)
                         .Select(e => new SelectListItem
                         {
                             Value = e.IdEspecialidad.ToString(),
                             Text = e.Nombre
                         }).ToList();
            }
        }

        private List<SelectListItem> ObtenerModalidades()
        {
            using (var db = new SIGEPEntities())
            {
                return db.ModalidadesTB
                         .OrderBy(m => m.Descripcion)
                         .Select(m => new SelectListItem
                         {
                             Value = m.IdModalidad.ToString(),
                             Text = m.Descripcion
                         }).ToList();
            }
        }

        private List<SelectListItem> ObtenerEmpresas()
        {
            using (var db = new SIGEPEntities())
            {
                return db.EmpresasTB
                         .OrderBy(e => e.NombreEmpresa)
                         .Select(e => new SelectListItem
                         {
                             Value = e.IdEmpresa.ToString(),
                             Text = e.NombreEmpresa
                         }).ToList();
            }
        }

        private List<SelectListItem> ObtenerEstados()
        {
            using (var db = new SIGEPEntities())
            {
                return db.EstadosTB
                         .OrderBy(s => s.Descripcion)
                         .Select(s => new SelectListItem
                         {
                             Value = s.IdEstado.ToString(),
                             Text = s.Descripcion
                         }).ToList();
            }
        }

        // ==============================
        // get ubicacion de empresa por idEmpresa
        // ==============================
        [HttpGet]
        public JsonResult GetUbicacionEmpresa(int idEmpresa)
        {
            using (var db = new SIGEPEntities())
            {
                // Busca la empresa y su dirección (si existe)
                var ubicacion = (from e in db.EmpresasTB
                                 join d in db.DireccionesTB on e.IdDireccion equals d.IdDireccion into jd
                                 from d in jd.DefaultIfEmpty()
                                 where e.IdEmpresa == idEmpresa
                                 select d.DireccionExacta).FirstOrDefault();

                return Json(new { ok = ubicacion != null, ubicacion = ubicacion ?? "" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult VistaVacantesProfesor()
        {
            using (var db = new SIGEPEntities())
            {
                var vacantes = db.VacantesPracticasTB
                    .Include("EmpresasTB")
                    .Include("ModalidadesTB")
                    .Include("EstadosTB")
                    .ToList();

                // Usuarios
                var usuarios = db.UsuariosTB
                    .Select(u => new SelectListItem
                    {
                        Value = u.IdUsuario.ToString(),
                        Text = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2
                    }).ToList();
                ViewBag.Usuarios = usuarios;

                // Estados
                var estados = db.EstadosTB
                    .Select(e => new SelectListItem
                    {
                        Value = e.IdEstado.ToString(),
                        Text = e.Descripcion.ToString()
                    }).ToList();
                ViewBag.Estados = estados;

                return View(vacantes);
            }
        }

        [HttpGet]
        public ActionResult VisualizacionPostulacion(int idVacante, int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities()) // Cambia por tu contexto
                {
                    // Obtener datos principales con el SP
                    var datosPractica = dbContext.ObtenerVisualizacionPracticaSP(idVacante, idUsuario).FirstOrDefault();

                    if (datosPractica == null)
                    {
                        ViewBag.Error = "No se encontró información de la práctica.";
                        return View(new VacantePracticaVM());
                    }

                    // Mapear directamente el resultado del SP al ViewModel
                    var viewModel = new VacantePracticaVM
                    {
                        // Datos de la Vacante
                        IdVacante = datosPractica.IdVacante,
                        Nombre = datosPractica.Nombre,
                        EmpresaNombre = datosPractica.EmpresaNombre,
                        Requerimientos = datosPractica.Requerimientos,
                        FechaMaxAplicacion = datosPractica.FechaMaxAplicacion,
                        ModalidadNombre = datosPractica.ModalidadNombre,

                        // Datos del Estudiante
                        IdUsuario = datosPractica.IdUsuario,
                        EstudianteNombre = datosPractica.EstudianteNombre,
                        EstudianteCedula = datosPractica.EstudianteCedula,
                        EstudianteCorreo = datosPractica.EstudianteCorreo,
                        EstudianteEdad = datosPractica.EstudianteEdad,
                        EstudianteEspecialidad = datosPractica.EstudianteEspecialidad,

                        // Datos de Contacto
                        ContactoEmpresaNombre = datosPractica.ContactoEmpresaNombre,
                        ContactoEmpresaEmail = datosPractica.ContactoEmpresaEmail,
                        ContactoEmpresaTelefono = datosPractica.ContactoEmpresaTelefono,

                        // Datos de la Práctica
                        FechaAplicacion = datosPractica.FechaAplicacion,
                        EstadoPractica = datosPractica.EstadoPractica,

                        ListaEstados = dbContext.EstadosTB
                    .Select(e => new EstadoVM { IdEstado = e.IdEstado, Descripcion = e.Descripcion })
                    .ToList()
                    };

                    // Obtener comentarios con el segundo SP
                    var comentarios = dbContext.ObtenerComentariosPracticaSP(idVacante, idUsuario)
                        .Select(c => new ComentarioVM
                        {
                            Id = c.Id,
                            Fecha = c.Fecha,
                            Usuario = c.Usuario,
                            Comentario = c.Comentario
                        }).ToList();

                    viewModel.Comentarios = comentarios;

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar la información: " + ex.Message;
                // Opcional: Registrar error como lo haces en tu proyecto
                // Utilitarios.RegistrarError(ex, (int?)Session["idUsuario"]);
                return View(new VacantePracticaVM());
            }
        }

        [HttpPost]
        public ActionResult AgregarComentario(int idVacante, int idUsuario, string comentario)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                int idUsuarioComentario = Convert.ToInt32(Session["IdUsuario"]);

                if (string.IsNullOrWhiteSpace(comentario))
                {
                    return Json(new { success = false, message = "El comentario no puede estar vacío" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    var resultado = dbContext.InsertarComentarioPracticaSP(idVacante, idUsuario, comentario, idUsuarioComentario).FirstOrDefault();

                    if (resultado == 1)
                    {
                        return Json(new { success = true, message = "Comentario agregado correctamente" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se pudo agregar el comentario" });
                    }
                }
            }
            catch (Exception ex)
            {
                // Registrar error si tienes utilidades para ello
                // Utilitarios.RegistrarError(ex, (int?)Session["IdUsuario"]);
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ActualizarEstadoPractica(int idPractica, int idEstado, string comentario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    // Ejecutar SP mapeado
                    var datosPractica = dbContext.ActualizarEstadoPracticaSP(idPractica, idEstado, comentario)
                                                  .FirstOrDefault();

                    if (datosPractica == null)
                    {
                        return Json(new { success = false, message = "No se encontró la práctica." });
                    }

                    // Crear ViewModel para devolver o usar en la vista
                    var viewModel = new VacantePracticaVM
                    {
                        IdPractica = datosPractica.IdPractica,
                        IdVacante = datosPractica.IdVacante,
                        IdUsuario = datosPractica.IdUsuario,
                        EstudianteNombre = datosPractica.EstudianteNombre,
                        EstudianteCorreo = datosPractica.EstudianteCorreo,
                        EstadoDescripcion = datosPractica.EstadoDescripcion,
                        UltimoComentario = datosPractica.Comentario,
                        FechaUltimoComentario = datosPractica.FechaComentario,
                        ListaEstados = dbContext.EstadosTB
                    .Select(e => new EstadoVM { IdEstado = e.IdEstado, Descripcion = e.Descripcion })
                    .ToList()
                    };

                    // Enviar correo al estudiante
                    if (!string.IsNullOrEmpty(viewModel.EstudianteCorreo))
                    {
                        string remitente = ConfigurationManager.AppSettings["CorreoRemitente"];
                        string password = ConfigurationManager.AppSettings["CorreoPassword"];

                        MailMessage mail = new MailMessage();
                        mail.From = new MailAddress(remitente);
                        mail.To.Add(viewModel.EstudianteCorreo);
                        mail.Subject = "Actualización de estado de práctica";
                        mail.Body = $"Hola {viewModel.EstudianteNombre},\n\n" +
                                    $"Tu práctica (ID {viewModel.IdPractica}) ha cambiado al estado: {viewModel.EstadoDescripcion}.\n" +
                                    $"Comentario: {viewModel.UltimoComentario}\n\nSaludos.";

                        SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                        smtp.Credentials = new NetworkCredential(remitente, password);
                        smtp.EnableSsl = true;
                        smtp.Send(mail);
                    }

                    return Json(new { success = true, data = viewModel, message = "Estado actualizado y correo enviado." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetVacantesProfesor(string estado = "", int idModalidad = 0)
        {
            using (var db = new SIGEPEntities())
            {
                // 1️⃣ Validar sesión de profesor
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { ok = false, mensaje = "Sesión expirada o no hay profesor logueado." }, JsonRequestBehavior.AllowGet);
                }

                int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

                // 2️⃣ Obtener especialidades del profesor
                var especialidadesProfesor = db.UsuarioEspecialidadTB
                    .Where(ue => ue.IdUsuario == idProfesor && ue.IdEstado == 1)
                    .Select(ue => ue.IdEspecialidad)
                    .ToList();

                if (!especialidadesProfesor.Any())
                {
                    return Json(new { ok = false, mensaje = "El profesor no tiene especialidades registradas." }, JsonRequestBehavior.AllowGet);
                }

                // 3️⃣ Vacantes filtradas solo por las especialidades del profesor
                var query = from v in db.VacantesPracticasTB
                            join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()
                            join d in db.DireccionesTB on e.IdDireccion equals d.IdDireccion into jd
                            from d in jd.DefaultIfEmpty()
                            join es in db.EstadosTB on v.IdEstado equals es.IdEstado into jes
                            from es in jes.DefaultIfEmpty()
                            join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante into jev
                            from ev in jev.DefaultIfEmpty()
                            join sp in db.EspecialidadesTB on ev.IdEspecialidad equals sp.IdEspecialidad into jsp
                            from sp in jsp.DefaultIfEmpty()
                            join m in db.ModalidadesTB on v.IdModalidad equals m.IdModalidad into jm
                            from m in jm.DefaultIfEmpty()
                            where especialidadesProfesor.Contains(ev.IdEspecialidad) // 🔹 filtro por especialidad del profesor
                            select new VacantePracticaDTO
                            {
                                IdVacante = v.IdVacante,
                                Nombre = v.Nombre,
                                IdEmpresa = v.IdEmpresa,
                                EmpresaNombre = e != null ? e.NombreEmpresa : "",
                                Requerimientos = v.Requerimientos,
                                FechaMaxAplicacion = v.FechaMaxAplicacion,
                                NumCupos = v.NumCupos ?? 0,
                                FechaCierre = v.FechaCierre,
                                IdModalidad = v.IdModalidad ?? 0,
                                ModalidadNombre = m != null ? m.Descripcion : "",
                                Descripcion = v.Descripcion,
                                IdEspecialidad = ev != null ? ev.IdEspecialidad : 0,
                                EspecialidadNombre = sp != null ? sp.Nombre : "",
                                IdEstado = v.IdEstado,
                                EstadoNombre = es != null ? es.Descripcion : "",
                                EstudiantesPostulados = db.PracticaEstudianteTB.Count(p => p.IdVacante == v.IdVacante)
                            };

                //Excluir vacantes Inactivas
                query = query.Where(x => x.EstadoNombre != "Inactivo");

                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(x => x.EstadoNombre == estado);

                if (idModalidad > 0)
                    query = query.Where(x => x.IdModalidad == idModalidad);

                var list = query.OrderByDescending(x => x.IdVacante).ToList();

                return Json(new { ok = true, data = list }, JsonRequestBehavior.AllowGet);
            }
        }



    }
}
