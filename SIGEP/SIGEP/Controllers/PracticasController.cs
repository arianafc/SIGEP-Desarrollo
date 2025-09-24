using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class PracticasController : Controller
    {
        // ==============================
        // VISTA PRINCIPAL VACANTES
        // ==============================
        [HttpGet]
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
                // Consulta principal (produce VacantePracticaDTO en memoria)
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
                                Ubicacion = d != null ? d.DireccionExacta : "",
                                EstudiantesPostulados = db.PracticaEstudianteTB.Count(p => p.IdVacante == v.IdVacante)
                            };

                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(x => x.EstadoNombre == estado);

                if (idEspecialidad > 0)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidad);

                if (idModalidad > 0)
                    query = query.Where(x => x.IdModalidad == idModalidad);

                var list = query.OrderByDescending(x => x.IdVacante).ToList();

                // Formatear fechas a ISO (string) para que el JS las pueda parsear con split('T')[0]
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
                    x.Ubicacion,
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

                             // 🔹 Join con especialidad
                         join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante into jev
                         from ev in jev.DefaultIfEmpty()

                         join esp in db.EspecialidadesTB on ev.IdEspecialidad equals esp.IdEspecialidad into jesp
                         from esp in jesp.DefaultIfEmpty()

                         where v.IdVacante == id
                         select new
                         {
                             v.IdVacante,
                             v.Nombre,
                             v.IdEmpresa,
                             EmpresaNombre = e != null ? e.NombreEmpresa : "",
                             NombreContacto = e != null ? e.NombreContacto : "",
                             v.Requerimientos,
                             v.FechaMaxAplicacion,
                             v.NumCupos,
                             v.FechaCierre,
                             v.Descripcion,
                             v.IdModalidad,
                             ModalidadNombre = m != null ? m.Descripcion : "",
                             ev.IdEspecialidad,
                             EspecialidadNombre = esp != null ? esp.Nombre : "",
                             v.IdEstado,
                             EstadoNombre = es != null ? es.Descripcion : "",
                             Ubicacion = dir != null ? dir.DireccionExacta : "",

                             // 🔹 Correos relacionados con la empresa
                             Emails = db.EmailsTB
                                 .Where(em => em.IdEmpresa == e.IdEmpresa)
                                 .Select(em => em.Email)
                                 .ToList(),

                             // 🔹 Teléfonos relacionados con la empresa
                             Telefonos = db.TelefonosTB
                                 .Where(t => t.IdEmpresa == e.IdEmpresa)
                                 .Select(t => t.Telefono)
                                 .ToList()
                         })
                         .AsEnumerable()
                         .Select(x => new
                         {
                             x.IdVacante,
                             x.Nombre,
                             x.IdEmpresa,
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
                             x.IdModalidad,
                             x.ModalidadNombre,
                             x.IdEspecialidad,
                             x.EspecialidadNombre,
                             x.IdEstado,
                             x.EstadoNombre,
                             x.Ubicacion,
                             x.Emails,
                             x.Telefonos
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

            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == model.IdVacante);
                    if (vacante == null)
                        return Json(new { ok = false, message = "Vacante no encontrada" });

                    // Actualizar solamente los campos permitidos aquí (NO tocar IdEstado)
                    vacante.Nombre = model.Nombre;
                    vacante.IdEmpresa = model.IdEmpresa;
                    // <- no tocar vacante.IdEstado (lo gestiona otro módulo)
                    vacante.Requerimientos = model.Requerimientos;
                    vacante.FechaMaxAplicacion = model.FechaMaxAplicacion;
                    vacante.NumCupos = model.NumCupos;
                    vacante.FechaCierre = model.FechaCierre;
                    vacante.IdModalidad = model.IdModalidad;
                    vacante.Descripcion = model.Descripcion;

                    db.SaveChanges();

                    // Reemplazar especialidad (tabla intermedia)
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
            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var especialidades = db.EspecialidadesVacantesTB.Where(ev => ev.IdVacante == id).ToList();
                    if (especialidades.Any())
                        db.EspecialidadesVacantesTB.RemoveRange(especialidades);

                    var postulaciones = db.PracticaEstudianteTB.Where(p => p.IdVacante == id).ToList();
                    if (postulaciones.Any())
                        db.PracticaEstudianteTB.RemoveRange(postulaciones);

                    var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == id);
                    if (vacante != null)
                        db.VacantesPracticasTB.Remove(vacante);

                    db.SaveChanges();
                    tx.Commit();

                    return Json(new { ok = true, message = "Vacante eliminada correctamente." });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new { ok = false, message = "Error: " + ex.Message });
                }
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
        [HttpGet]
        public JsonResult ObtenerEstudiantesParaAsignar(int idVacante)
        {
            using (var db = new SIGEPEntities())
            {
                // Buscar estado "Asignado" o "Asignada"
                var estadoAsignado = db.EstadosTB
                    .FirstOrDefault(e => e.Descripcion == "Asignado" || e.Descripcion == "Asignada");

                int? idEstadoAsignado = estadoAsignado?.IdEstado; // 👈 ya lo tenemos en un int?

                var lista = (from u in db.UsuariosTB
                             where !db.PracticaEstudianteTB.Any(p =>
                                  p.IdUsuario == u.IdUsuario &&
                                  p.IdVacante == idVacante &&
                                  (idEstadoAsignado != null && p.IdEstado == idEstadoAsignado))
                             orderby u.Nombre
                             select new
                             {
                                 IdEstudiante = u.IdUsuario,
                                 NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                 Cedula = u.Cedula,
                                 Asignada = idEstadoAsignado != null &&
                                            db.PracticaEstudianteTB.Any(p =>
                                                p.IdUsuario == u.IdUsuario &&
                                                p.IdEstado == idEstadoAsignado)
                             })
                             .ToList();

                return Json(new { ok = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }




        // ==============================
        // ASIGNAR ESTUDIANTE A VACANTE (POST)
        // ==============================
        // Nombre antiguo: Asignar (seguimos manteniéndolo)
        [HttpPost]
        public JsonResult Asignar(int idVacante, int idUsuario)
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

        // Alias con el nombre que tu JS podría estar usando: AsignarEstudiante
        [HttpPost]
        public JsonResult AsignarEstudiante(int idVacante, int idEstudiante)
        {
            // solo un wrapper para evitar que JS y controlador se desincronicen
            return Asignar(idVacante, idEstudiante);
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



    }
}
