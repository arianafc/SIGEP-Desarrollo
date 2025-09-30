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
using System.Text;
using System.Web.Mvc;


namespace SIGEP.Controllers
{
    //[FiltroProfesor]
    
    public class PracticasController : Controller
    {

        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();

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
            using (var db = new SIGEPEntities())
            {
                // IDs de estados "activos" en cualquier vacante
                int[] estadosActivos = { 3, 5, 6, 12, 7 }; // En progreso, Asignada, Aprobada, Pendiente de Aprobación

                // Solo usuarios con rol "Estudiante" + LEFT JOIN a especialidades activas
                var estudiantes = (
                    from u in db.UsuariosTB
                    join r in db.RolesTB on u.IdRol equals r.IdRol
                    where r.Descripcion == "Estudiante"
                    join ue in db.UsuarioEspecialidadTB.Where(x => x.IdEstado == 1)
                        on u.IdUsuario equals ue.IdUsuario into jue
                    from ue in jue.DefaultIfEmpty()
                    join esp in db.EspecialidadesTB
                        on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
                    from esp in jesp.DefaultIfEmpty()
                    group new { u, esp } by new { u.IdUsuario, u.Nombre, u.Apellido1, u.Apellido2, u.Cedula } into g
                    select new
                    {
                        IdUsuario = g.Key.IdUsuario,
                        NombreCompleto = g.Key.Nombre + " " + g.Key.Apellido1 + " " + g.Key.Apellido2,
                        Cedula = g.Key.Cedula,
                        // Usa "Nombre" si es tu columna; cambia a Descripcion si aplica en tu esquema
                        Especialidad = g.Select(x => x.esp != null ? x.esp.Nombre : "").FirstOrDefault()
                    }
                ).ToList();

                // Armar respuesta con relación en ESTA vacante y estado a mostrar
                var data = estudiantes.Select(e =>
                {
                    var rel = db.PracticaEstudianteTB
                        .Where(p => p.IdUsuario == e.IdUsuario && p.IdVacante == idVacante)
                        .OrderByDescending(p => p.IdPractica)
                        .FirstOrDefault();

                    bool tieneRelacion = rel != null;
                    string estadoVacante = tieneRelacion ? rel.EstadosTB.Descripcion : null;

                    bool tieneActivos = !tieneRelacion && db.PracticaEstudianteTB
                        .Any(p => p.IdUsuario == e.IdUsuario && estadosActivos.Contains(p.IdEstado));

                    string estadoMostrar = tieneRelacion
                        ? estadoVacante
                        : (tieneActivos ? "Con Procesos Activos" : "Sin Procesos Activos");

                    return new
                    {
                        e.IdUsuario,
                        e.NombreCompleto,
                        e.Cedula,
                        e.Especialidad,
                        TieneRelacionEnVacante = tieneRelacion,
                        EstadoVacante = estadoVacante,
                        EstadoMostrar = estadoMostrar
                    };
                }).ToList();

                return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ObtenerEstudiantesParaAsignar(int idVacante)
        {
            try
            {
                // 1) Especialidades requeridas por la vacante
                var especialidadesDeLaVacante = db.EspecialidadesVacantesTB
                    .Where(ev => ev.IdVacante == idVacante)
                    .Select(ev => ev.IdEspecialidad)
                    .ToList();

                // 2) Estudiantes activos con esas especialidades
                var baseEstudiantes = (
                    from u in db.UsuariosTB
                    join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
                    join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                    where
                        u.IdRol == 1                 // Estudiante
                        && u.IdEstado == 1           // Activo (o el estado que uses como "activo")
                        && ue.IdEstado == 1          // Relación usuario-especialidad activa
                        && especialidadesDeLaVacante.Contains(ue.IdEspecialidad)
                    select new
                    {
                        u.IdUsuario,
                        u.Nombre,
                        u.Apellido1,
                        u.Apellido2,
                        u.Cedula,
                        Especialidad = esp.Nombre
                    }
                ).Distinct().ToList();

                var idsEstudiantes = baseEstudiantes.Select(x => x.IdUsuario).ToList();

                // 3) Estados (globales y por vacante) usando PracticaEstudianteTB + EstadosTB
                //    - Global: si tiene alguna 'Asignada' en cualquier vacante
                //    - Por vacante: si ya tiene relación con ESTA vacante y en qué estado
                var practicasEstudiantes = (
                    from p in db.PracticaEstudianteTB
                    join e in db.EstadosTB on p.IdEstado equals e.IdEstado
                    where idsEstudiantes.Contains(p.IdUsuario)
                    select new
                    {
                        p.IdUsuario,
                        p.IdVacante,
                        p.IdPractica,
                        Estado = e.Descripcion
                    }
                ).ToList();

                // Precalcular “Asignada” global por estudiante
                var asignadaGlobal = practicasEstudiantes
                    .GroupBy(x => x.IdUsuario)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Any(r => r.Estado == "Asignada" || r.Estado == "Asignado")
                    );

                // Relación específica con ESTA vacante
                var porEstaVacante = practicasEstudiantes
                    .Where(x => x.IdVacante == idVacante)
                    .GroupBy(x => x.IdUsuario)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderByDescending(r => r.IdPractica) // por si hay histórico
                            .First()
                    );

                // 4) Proyección final al shape esperado por tu DataTable del modal
                var data = baseEstudiantes.Select(e => new
                {
                    IdEstudiante = e.IdUsuario,
                    NombreCompleto = $"{e.Nombre} {e.Apellido1} {e.Apellido2}",
                    e.Cedula,
                    e.Especialidad,

                    // Badge “Práctica Asignada” (global) vs “Sin Práctica Asignada”
                    Asignada = asignadaGlobal.TryGetValue(e.IdUsuario, out var tieneAsignada) && tieneAsignada,

                    // Estado concreto en ESTA vacante (si ya tuvo o tiene proceso)
                    TieneRelacionEnVacante = porEstaVacante.ContainsKey(e.IdUsuario),
                    EstadoVacante = porEstaVacante.ContainsKey(e.IdUsuario) ? porEstaVacante[e.IdUsuario].Estado : null,
                    IdPracticaVacante = porEstaVacante.ContainsKey(e.IdUsuario) ? (int?)porEstaVacante[e.IdUsuario].IdPractica : null
                })
                .OrderBy(x => x.NombreCompleto)
                .ToList();

                return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }





        // ==============================
        // ASIGNAR ESTUDIANTE A VACANTE 
        [HttpPost]
        public JsonResult AsignarEstudiante(int idVacante, int idUsuario)
        {
            using (var db = new SIGEPEntities())
            {
                // 🔹 Aquí buscamos "En proceso" en lugar de "Asignado/Asignada"
                var estadoEnProgreso = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion == "En progreso");

                if (estadoEnProgreso == null)
                    return Json(new { ok = false, message = "El estado 'En progreso' no existe en EstadosTB" },
                                JsonRequestBehavior.AllowGet);

                var existente = db.PracticaEstudianteTB
                    .FirstOrDefault(p => p.IdVacante == idVacante && p.IdUsuario == idUsuario);

                if (existente != null)
                {
                    existente.IdEstado = estadoEnProgreso.IdEstado;
                    existente.FechaAplicacion = DateTime.Now;
                }
                else
                {
                    db.PracticaEstudianteTB.Add(new PracticaEstudianteTB
                    {
                        IdVacante = idVacante,
                        IdUsuario = idUsuario,
                        IdEstado = estadoEnProgreso.IdEstado,
                        FechaAplicacion = DateTime.Now
                    });
                }

                db.SaveChanges();
                return Json(new { ok = true, message = "Estudiante asignado en estado 'En progreso'." },
                            JsonRequestBehavior.AllowGet);
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
        // Retirar ESTUDIANTE de practica
        // ==============================

        // CAMBIAR ESTADO DE ESTUDIANTE A "RETIRADA"
        [HttpPost]
        public JsonResult RetirarEstudiante(int idVacante, int idUsuario)
        {
            using (var db = new SIGEPEntities())
            {
                // 🔹 Buscar estado "Retirada"
                var estadoRetirada = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion == "Retirada");

                if (estadoRetirada == null)
                    return Json(new { ok = false, message = "El estado 'Retirada' no existe en EstadosTB" },
                                JsonRequestBehavior.AllowGet);

                var existente = db.PracticaEstudianteTB
                    .FirstOrDefault(p => p.IdVacante == idVacante && p.IdUsuario == idUsuario);

                if (existente != null)
                {
                    // 🔹 Actualiza a "Retirada"
                    existente.IdEstado = estadoRetirada.IdEstado;
                    existente.FechaAplicacion = DateTime.Now;
                }
                else
                {
                    // 🔹 Si no existía, lo crea directamente en "Retirada"
                    db.PracticaEstudianteTB.Add(new PracticaEstudianteTB
                    {
                        IdVacante = idVacante,
                        IdUsuario = idUsuario,
                        IdEstado = estadoRetirada.IdEstado,
                        FechaAplicacion = DateTime.Now
                    });
                }

                db.SaveChanges();
                return Json(new { ok = true, message = "El estudiante ha sido marcado como 'Retirada'." },
                            JsonRequestBehavior.AllowGet);
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
                    .FirstOrDefault(e => e.Descripcion == "Retirada");

                if (estadoSinPractica == null)
                {
                    return Json(new { ok = false, mensaje = "No existe el estado 'Retirada' en la BD." }, JsonRequestBehavior.AllowGet);
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
                using (var dbContext = new SIGEPEntities())
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

                        // Datos de la Práctica - AGREGADO IdPractica
                        IdPractica = datosPractica.IdPractica,
                        FechaAplicacion = datosPractica.FechaAplicacion,
                        EstadoPractica = datosPractica.EstadoPractica,

                        // Cargar lista de estados para el dropdown
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
                    // Ejecutar el SP
                    var resultado = dbContext.InsertarComentarioPracticaSP(idVacante, idUsuario, comentario, idUsuarioComentario).FirstOrDefault();

                    // Verificar si se insertó al menos una fila
                    if (resultado > 0)
                    {
                        return Json(new { success = true, message = "Comentario agregado correctamente" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "No se pudo agregar el comentario. Verifique que la práctica exista." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ActualizarEstadoPractica(int idPractica, int idEstado, string comentario)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    // Ejecutar SP para actualizar estado
                    int idUsuarioSesion = Convert.ToInt32(Session["IdUsuario"]);

                    var resultado = dbContext.ActualizarEstadoPracticaSP(
                        idPractica,
                        idEstado,
                        comentario,
                        idUsuarioSesion  // Agregar este parámetro
                    ).FirstOrDefault();

                    if (resultado == null)
                    {
                        return Json(new { success = false, message = "No se encontró la práctica." });
                    }

                    // Crear el correo HTML usando tu formato existente
                    if (!string.IsNullOrEmpty(resultado.EstudianteCorreo))
                    {
                        try
                        {
                            StringBuilder mensaje = new StringBuilder();
                            mensaje.Append("<!DOCTYPE html>");
                            mensaje.Append("<html lang='es'>");
                            mensaje.Append("<head><meta charset='UTF-8'></head>");
                            mensaje.Append("<body style='margin:0; padding:0; font-family: Arial, sans-serif; background-color:#f4f4f4;'>");
                            mensaje.Append("<table align='center' width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.1);'>");

                            // Encabezado
                            mensaje.Append("<tr>");
                            mensaje.Append("<td align='center' style='background-color:#2d594d; padding:20px;'>");
                            mensaje.Append("<h2 style='color:#ffffff; margin:0; font-size:22px;'>Actualización de Estado de Práctica</h2>");
                            mensaje.Append("</td>");
                            mensaje.Append("</tr>");

                            // Contenido
                            mensaje.Append("<tr>");
                            mensaje.Append("<td style='padding:30px; color:#333333; font-size:15px; line-height:1.6;'>");
                            mensaje.Append("Estimado(a) <strong>" + resultado.EstudianteNombre + "</strong>,<br><br>");
                            mensaje.Append("Le informamos que su práctica profesional ha cambiado de estado.<br><br>");
                            mensaje.Append("<table style='width:100%; border-collapse:collapse; margin:20px 0;'>");
                            mensaje.Append("<tr style='background-color:#f8f9fa;'>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6; font-weight:bold; color:#2d594d;'>Nuevo Estado:</td>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6;'>" + resultado.EstadoDescripcion + "</td>");
                            mensaje.Append("</tr>");
                            mensaje.Append("<tr>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6; font-weight:bold; color:#2d594d;'>Comentario:</td>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6;'>" + resultado.Comentario + "</td>");
                            mensaje.Append("</tr>");
                            mensaje.Append("<tr style='background-color:#f8f9fa;'>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6; font-weight:bold; color:#2d594d;'>Fecha de Actualización:</td>");
                            mensaje.Append("<td style='padding:12px; border:1px solid #dee2e6;'>" + resultado.FechaComentario?.ToString("dd/MM/yyyy HH:mm") + "</td>");
                            mensaje.Append("</tr>");
                            mensaje.Append("</table>");
                            mensaje.Append("Para más información, puede ingresar al sistema SIGEP.<br><br>");
                            mensaje.Append("Saludos cordiales,<br>");
                            mensaje.Append("<strong>Sistema SIGEP</strong>");
                            mensaje.Append("</td>");
                            mensaje.Append("</tr>");

                            // Pie
                            mensaje.Append("<tr>");
                            mensaje.Append("<td align='center' style='background-color:#f0f0f0; padding:15px; font-size:12px; color:#666666;'>");
                            mensaje.Append("© 2025 SIGEP. Todos los derechos reservados.");
                            mensaje.Append("</td>");
                            mensaje.Append("</tr>");
                            mensaje.Append("</table>");
                            mensaje.Append("</body>");
                            mensaje.Append("</html>");

                            // Enviar correo usando tu método utilitario
                            bool correoEnviado = utilitarios.EnviarCorreo(
                                resultado.EstudianteCorreo,
                                mensaje.ToString(),
                                "Actualización de Estado de Práctica - SIGEP"
                            );

                            if (correoEnviado)
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Estado actualizado correctamente y notificación enviada por correo.",
                                    data = new
                                    {
                                        estado = resultado.EstadoDescripcion,
                                        comentario = resultado.Comentario,
                                        fecha = resultado.FechaComentario?.ToString("dd/MM/yyyy HH:mm")
                                    }
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Estado actualizado correctamente, pero no se pudo enviar el correo de notificación.",
                                    data = new
                                    {
                                        estado = resultado.EstadoDescripcion,
                                        comentario = resultado.Comentario,
                                        fecha = resultado.FechaComentario?.ToString("dd/MM/yyyy HH:mm")
                                    }
                                });
                            }
                        }
                        catch (Exception emailEx)
                        {
                            // Error en el envío de correo, pero el estado se actualizó
                            return Json(new
                            {
                                success = true,
                                message = "Estado actualizado correctamente, pero ocurrió un error al enviar el correo: " + emailEx.Message,
                                data = new
                                {
                                    estado = resultado.EstadoDescripcion,
                                    comentario = resultado.Comentario,
                                    fecha = resultado.FechaComentario?.ToString("dd/MM/yyyy HH:mm")
                                }
                            });
                        }
                    }
                    else
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Estado actualizado correctamente, pero no se encontró correo del estudiante.",
                            data = new
                            {
                                estado = resultado.EstadoDescripcion,
                                comentario = resultado.Comentario,
                                fecha = resultado.FechaComentario?.ToString("dd/MM/yyyy HH:mm")
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        [HttpGet]
        public ActionResult PracticasCoordinador()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Home");

            return View(); // buscará Views/Practicas/PracticasCoordinador.cshtml
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

        [HttpGet]
        public ActionResult MiPractica()
        {
            if (Session["IdUsuario"] == null)
            {
                return RedirectToAction("IniciarSesion", "Home");
            }

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);

            using (var dbContext = new SIGEPEntities())
            {
                var practica = dbContext.PracticaEstudianteTB
                    .Where(p => p.IdUsuario == idUsuario)
                    .FirstOrDefault();

                if (practica == null)
                {
                    // Usar TempData para mostrar SweetAlert en el Index
                    TempData["SwalWarning"] = "Aún no tienes una práctica asignada. Mantente atento a las actualizaciones del sistema.";
                    return RedirectToAction("Index", "Home");
                }

                // Redirigir al action existente con los parámetros necesarios
                return RedirectToAction("VisualizacionPostulacion", new
                {
                    idVacante = practica.IdVacante,
                    idUsuario = practica.IdUsuario
                });
            }
        }

        [HttpGet]
        public ActionResult PostulacionesEstudiantes()
        {
            if (Session["IdUsuario"] == null)
            {
                return RedirectToAction("IniciarSesion", "Home");
            }

            try
            {
                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var postulaciones = dbContext.ObtenerPostulacionesEstudianteSP(idUsuario)
                        .Select(p => new PostulacionEstudianteVM
                        {
                            IdPractica = p.IdPractica,
                            IdVacante = p.IdVacante,
                            IdUsuario = p.IdUsuario,
                            NombreVacante = p.NombreVacante,
                            NombreEmpresa = p.NombreEmpresa,
                            EstadoPractica = p.EstadoPractica,
                            FechaAplicacion = p.FechaAplicacion,
                            EsAutogestionada = p.EsAutogestionada ?? false
                        }).ToList();

                    var viewModel = new MisPostulacionesVM
                    {
                        Postulaciones = postulaciones
                    };

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al cargar las postulaciones: " + ex.Message;
                return View(new MisPostulacionesVM());
            }
        }


        [HttpGet]
        public ActionResult ListadoEstudiantes()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Home");

            return View(); // la vista que te dejo abajo
        }

        // === API para DataTables (AJAX) ===
        // Devuelve estudiantes (de las especialidades del profesor) y su situación global de práctica,
        // más datos de contacto/empresa y relación con alguna vacante reciente.
        [HttpGet]
        public JsonResult ListarEstudiantesJson(int? idVacante = null)
        {
            if (Session["IdUsuario"] == null)
                return Json(new { data = new object[0] }, JsonRequestBehavior.AllowGet);

            int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

            // 1) Traer base desde SP (ya lo tienes en BDD)
            //    SELECT global: EstadoPractica (Asignada / Con Procesos Activos / Sin Procesos Activos),
            //    EstadoUsuario, Especialidad, TieneRelacionEnVacante, EstadoVacante, IdPracticaVacante
            var lista = db.ObtenerEstudiantesProfesorSP(idProfesor, idVacante).ToList();

            // 2) Proyectar con datos adicionales (teléfono y última empresa)
            var rows = lista.Select(x => new EstudianteListadoVM
            {
                IdUsuario = x.IdUsuario,
                Cedula = x.Cedula,
                Nombre = x.Nombre,
                Especialidad = x.Especialidad,

                Telefono = db.TelefonosTB
                 .Where(t => t.IdUsuario == x.IdUsuario)
                 .Select(t => t.Telefono)
                 .FirstOrDefault(),

                EstadoPostulacion = x.EstadoPractica,
                Empresa = UltimaEmpresa(x.IdUsuario),

                // ⬇️ Normalizamos el nullable a bool y además caemos a IdPracticaVacante si hace falta
                TieneRelacionEnVacante =
        ( /* si existe en el SP */ (bool?)(x.TieneRelacionEnVacante ?? null) ?? false)
        || (x.IdPracticaVacante != null),

                // Usa el valor de EstadoVacante si viene; si no, muestra “Con relación” si hay relación
                Tipo = !string.IsNullOrEmpty(x.EstadoVacante)
            ? x.EstadoVacante
            : (((bool?)(x.TieneRelacionEnVacante ?? null) ?? false) || (x.IdPracticaVacante != null) ? "Con relación" : "—"),

                IdPracticaVacante = x.IdPracticaVacante,
                EstadoVacante = x.EstadoVacante,
                IdVacanteUltima = UltimaVacanteId(x.IdUsuario)
            }).ToList();


            return Json(new { data = rows }, JsonRequestBehavior.AllowGet);
        }

        private string UltimaEmpresa(int idUsuario)
        {
            var q = from p in db.PracticaEstudianteTB
                    join v in db.VacantesPracticasTB on p.IdVacante equals v.IdVacante
                    join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa
                    where p.IdUsuario == idUsuario
                    orderby p.FechaAplicacion descending, p.IdPractica descending
                    select e.NombreEmpresa;
            return q.FirstOrDefault();
        }

        private int? UltimaVacanteId(int idUsuario)
        {
            var q = from p in db.PracticaEstudianteTB
                    where p.IdUsuario == idUsuario
                    orderby p.FechaAplicacion descending, p.IdPractica descending
                    select (int?)p.IdVacante;
            return q.FirstOrDefault();
        }

        // === Desasignar/retirar práctica (usa SP de actualización de estado) ===
        [HttpPost]
        public JsonResult DesasignarPractica(int idPractica, string comentario)
        {
            try
            {
                // Buscar estado "Retirada" o "Cancelada" (usa el que exista en EstadosTB)
                int idEstado = EstadoIdPorDescripcion(new[] { "Retirada", "Cancelada" });

                db.Database.ExecuteSqlCommand(
                    "EXEC dbo.ActualizarEstadoPracticaSP @IdPractica, @IdEstado, @Comentario",
                    new SqlParameter("@IdPractica", idPractica),
                    new SqlParameter("@IdEstado", idEstado),
                    new SqlParameter("@Comentario", (object)comentario ?? DBNull.Value)
                );

                return Json(new { ok = true, msg = "Práctica desasignada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        // === Cambiar estado académico del usuario (actualiza UsuariosTB.IdEstado) ===
        [HttpPost]
        public JsonResult CambiarEstadoAcademico(int idUsuario, string nuevoEstado)
        {
            try
            {
                int idEstado = EstadoIdPorDescripcion(new[] { nuevoEstado }); // p.ej. "Aprobado" o "Rezagado"
                var u = db.UsuariosTB.Find(idUsuario);
                if (u == null) return Json(new { ok = false, msg = "Usuario no encontrado." });

                u.IdEstado = idEstado;
                db.SaveChanges();

                return Json(new { ok = true, msg = "Estado académico actualizado." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        // === Visualización de la postulación (usa SP que ya tienes) ===
        
        // Helpers

        private int EstadoIdPorDescripcion(IEnumerable<string> descripciones)
        {
            var set = descripciones.Select(d => d.Trim()).ToList();
            var estado = db.EstadosTB.FirstOrDefault(e => set.Contains(e.Descripcion));
            if (estado == null)
                throw new Exception("No se encontró un estado válido en EstadosTB: " + string.Join(" / ", set));
            return estado.IdEstado;
        }

        // DTO para DataTables
        public class EstudianteListadoVM
        {
            public int IdUsuario { get; set; }
            public string Cedula { get; set; }
            public string Nombre { get; set; }
            public string Especialidad { get; set; }
            public string Telefono { get; set; }
            public string EstadoPostulacion { get; set; }
            public string Empresa { get; set; }
            public string Tipo { get; set; }
            public int? IdPracticaVacante { get; set; }
            public string EstadoVacante { get; set; }
            public int? IdVacanteUltima { get; set; }

            // ⬇️ NUEVA: para evitar el "no existe en el contexto actual"
            public bool TieneRelacionEnVacante { get; set; }
        }


        // ViewModel para la Visualización (propiedades que devuelve tu SP)
        public class VisualizacionVM
        {
            public int IdVacante { get; set; }
            public string Nombre { get; set; }
            public string EmpresaNombre { get; set; }
            public string Requerimientos { get; set; }
            public DateTime? FechaMaxAplicacion { get; set; }
            public string ModalidadNombre { get; set; }

            public int IdUsuario { get; set; }
            public string EstudianteNombre { get; set; }
            public string EstudianteCedula { get; set; }
            public int EstudianteEdad { get; set; }
            public string EstudianteEspecialidad { get; set; }
            public string EstudianteCorreo { get; set; }

            public string ContactoEmpresaNombre { get; set; }
            public string ContactoEmpresaEmail { get; set; }
            public string ContactoEmpresaTelefono { get; set; }

            public DateTime? FechaAplicacion { get; set; }
            public string EstadoPractica { get; set; }
        }


        [HttpPost]
        public ActionResult RegistrarAutogestion(AutogestionPracticaVM model)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(model.NombreEmpresa) ||
                    string.IsNullOrWhiteSpace(model.Sector) ||
                    string.IsNullOrWhiteSpace(model.NombreEncargado) ||
                    string.IsNullOrWhiteSpace(model.Puesto) ||
                    string.IsNullOrWhiteSpace(model.Correo) ||
                    string.IsNullOrWhiteSpace(model.Telefono) ||
                    string.IsNullOrWhiteSpace(model.Provincia) ||
                    string.IsNullOrWhiteSpace(model.Canton) ||
                    string.IsNullOrWhiteSpace(model.Distrito) ||
                    string.IsNullOrWhiteSpace(model.DireccionExacta) ||
                    string.IsNullOrWhiteSpace(model.DescripcionTareas) ||
                    string.IsNullOrWhiteSpace(model.Duracion) ||
                    model.IdModalidad == 0)
                {
                    return Json(new { success = false, message = "Todos los campos son obligatorios" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    // Validar que la modalidad existe
                    var modalidad = dbContext.ModalidadesTB.FirstOrDefault(m => m.IdModalidad == model.IdModalidad);
                    if (modalidad == null)
                    {
                        return Json(new { success = false, message = "La modalidad seleccionada no es válida" });
                    }

                    // Verificar que existe al menos un estado activo
                    var estadoActivo = dbContext.EstadosTB.FirstOrDefault(e => e.IdEstado == 1);
                    if (estadoActivo == null)
                    {
                        // Crear estado activo si no existe
                        var nuevoEstado = new EstadosTB { Descripcion = "Activo" };
                        dbContext.EstadosTB.Add(nuevoEstado);
                        dbContext.SaveChanges();
                        estadoActivo = nuevoEstado;
                    }

                    // Buscar o crear provincia
                    var provincia = dbContext.ProvinciasTB.FirstOrDefault(p => p.Nombre == model.Provincia);
                    if (provincia == null)
                    {
                        provincia = new ProvinciasTB { Nombre = model.Provincia };
                        dbContext.ProvinciasTB.Add(provincia);
                        dbContext.SaveChanges();
                    }

                    // Buscar o crear cantón
                    var canton = dbContext.CantonesTB.FirstOrDefault(c => c.Nombre == model.Canton && c.IdProvincia == provincia.IdProvincia);
                    if (canton == null)
                    {
                        canton = new CantonesTB { Nombre = model.Canton, IdProvincia = provincia.IdProvincia };
                        dbContext.CantonesTB.Add(canton);
                        dbContext.SaveChanges();
                    }

                    // Buscar o crear distrito
                    var distrito = dbContext.DistritosTB.FirstOrDefault(d => d.Nombre == model.Distrito && d.IdCanton == canton.IdCanton);
                    if (distrito == null)
                    {
                        distrito = new DistritosTB { Nombre = model.Distrito, IdCanton = canton.IdCanton };
                        dbContext.DistritosTB.Add(distrito);
                        dbContext.SaveChanges();
                    }

                    // Crear la dirección
                    var direccion = new DireccionesTB
                    {
                        DireccionExacta = model.DireccionExacta.Trim(),
                        IdDistrito = distrito.IdDistrito,
                        IdEstado = estadoActivo.IdEstado
                    };

                    dbContext.DireccionesTB.Add(direccion);
                    dbContext.SaveChanges();

                    // Crear la empresa
                    var empresa = new EmpresasTB
                    {
                        NombreEmpresa = model.NombreEmpresa.Trim(),
                        NombreContacto = model.NombreEncargado.Trim(),
                        AreasAfines = model.Sector.Trim(),
                        IdDireccion = direccion.IdDireccion,
                        IdEstado = estadoActivo.IdEstado
                    };

                    dbContext.EmpresasTB.Add(empresa);
                    dbContext.SaveChanges();

                    // Buscar o crear estado "Pendiente de Aprobación"
                    var estadoPendiente = dbContext.EstadosTB.FirstOrDefault(e => e.Descripcion == "Pendiente de Aprobación");
                    if (estadoPendiente == null)
                    {
                        estadoPendiente = new EstadosTB { Descripcion = "Pendiente de Aprobación" };
                        dbContext.EstadosTB.Add(estadoPendiente);
                        dbContext.SaveChanges();
                    }

                    // Crear la vacante de práctica autogestionada
                    var vacante = new VacantesPracticasTB
                    {
                        Nombre = $"Práctica Autogestionada - {model.NombreEmpresa}",
                        IdEmpresa = empresa.IdEmpresa,
                        Descripcion = model.DescripcionTareas.Trim(),
                        Requerimientos = $"Duración: {model.Duracion}",
                        Tipo = "Autogestionada",
                        IdModalidad = model.IdModalidad,
                        IdEstado = 1,
                        FechaMaxAplicacion = DateTime.Now.AddDays(30),
                        NumCupos = 1
                    };

                    dbContext.VacantesPracticasTB.Add(vacante);
                    dbContext.SaveChanges();

                    // Crear la práctica del estudiante
                    var practica = new PracticaEstudianteTB
                    {
                        IdVacante = vacante.IdVacante,
                        IdUsuario = idUsuario,
                        FechaAplicacion = DateTime.Now, 
                        IdEstado = estadoPendiente.IdEstado
                    };

                    dbContext.PracticaEstudianteTB.Add(practica);

                    // Guardar email y teléfono de la empresa
                    var email = new EmailsTB
                    {
                        IdEmpresa = empresa.IdEmpresa,
                        Email = model.Correo.Trim()
                    };
                    dbContext.EmailsTB.Add(email);

                    var telefono = new TelefonosTB
                    {
                        IdEmpresa = empresa.IdEmpresa,
                        Telefono = model.Telefono.Trim()
                    };
                    dbContext.TelefonosTB.Add(telefono);

                    dbContext.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Práctica autogestionada registrada exitosamente. Pendiente de aprobación por coordinación."
                    });
                }
            }
            catch (Exception ex)
            {
                // Log más detallado del error
                System.Diagnostics.Debug.WriteLine($"Error en RegistrarAutogestion: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }
    }
}
