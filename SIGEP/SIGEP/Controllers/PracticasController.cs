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
        // GET: /Practicas/VacantesEstudiantes
        [HttpGet]
        public ActionResult VacantesEstudiantes()
        {
            // Cargar listas para los dropdowns
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
            using (var db = new SIGEPContext())
            {
                var query = from v in db.Vacantes
                            join e in db.Empresas on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()
                            join es in db.Estados on v.IdEstado equals es.IdEstado into jes
                            from es in jes.DefaultIfEmpty()
                            join ev in db.EspecialidadesVacantes on v.IdVacante equals ev.IdVacante into jev
                            from ev in jev.DefaultIfEmpty()
                            join sp in db.Especialidades on ev.IdEspecialidad equals sp.IdEspecialidad into jsp
                            from sp in jsp.DefaultIfEmpty()
                            join m in db.Modalidades on v.IdModalidad equals m.IdModalidad into jm
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
                                EstudiantesPostulados = db.PracticasEstudiantes.Count(p => p.IdVacante == v.IdVacante)
                            };

                // filtros
                if (!string.IsNullOrEmpty(estado))
                    query = query.Where(x => x.EstadoNombre == estado);

                if (idEspecialidad > 0)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidad);

                if (idModalidad > 0)
                    query = query.Where(x => x.IdModalidad == idModalidad);

                var list = query.OrderByDescending(x => x.IdVacante).ToList();
                return Json(new { data = list }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // CREAR VACANTE (POST)
        // ==============================
        [HttpPost]
        public JsonResult Crear(VacantePracticaVM model)
        {
            if (model == null)
                return Json(new { ok = false, message = "Modelo inválido" });

            using (var db = new SIGEPContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // Por defecto, si no recibe IdEstado, usar 1 (No asignada) - opcional
                    var idEstado = model.IdEstado > 0 ? model.IdEstado : 1;

                    var vacante = new VacantePractica
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

                    db.Vacantes.Add(vacante);
                    db.SaveChanges();

                    if (model.IdEspecialidad > 0)
                    {
                        var esp = new EspecialidadVacante
                        {
                            IdVacante = vacante.IdVacante,
                            IdEspecialidad = model.IdEspecialidad
                        };
                        db.EspecialidadesVacantes.Add(esp);
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
        [HttpGet]
        public JsonResult Detalle(int id)
        {
            using (var db = new SIGEPContext())
            {
                var data = (from v in db.Vacantes
                            join e in db.Empresas on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()
                            join es in db.Estados on v.IdEstado equals es.IdEstado into jes
                            from es in jes.DefaultIfEmpty()
                            join ev in db.EspecialidadesVacantes on v.IdVacante equals ev.IdVacante into jev
                            from ev in jev.DefaultIfEmpty()
                            join sp in db.Especialidades on ev.IdEspecialidad equals sp.IdEspecialidad into jsp
                            from sp in jsp.DefaultIfEmpty()
                            join m in db.Modalidades on v.IdModalidad equals m.IdModalidad into jm
                            from m in jm.DefaultIfEmpty()
                            where v.IdVacante == id
                            select new VacantePracticaVM
                            {
                                IdVacante = v.IdVacante,
                                Nombre = v.Nombre,
                                IdEmpresa = v.IdEmpresa,
                                EmpresaNombre = e != null ? e.NombreEmpresa : "",
                                IdEspecialidad = ev != null ? ev.IdEspecialidad : 0,
                                EspecialidadNombre = sp != null ? sp.Nombre : "",
                                IdModalidad = v.IdModalidad ?? 0,
                                ModalidadNombre = m != null ? m.Descripcion : "",
                                NumCupos = v.NumCupos ?? 0,
                                FechaMaxAplicacion = v.FechaMaxAplicacion,
                                FechaCierre = v.FechaCierre,
                                Requerimientos = v.Requerimientos,
                                Descripcion = v.Descripcion,
                                IdEstado = v.IdEstado,
                                EstadoNombre = es != null ? es.Descripcion : ""
                            }).FirstOrDefault();

                return Json(new { ok = data != null, data }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // EDITAR VACANTE (POST)
        // ==============================
        [HttpPost]
        public JsonResult Editar(VacantePracticaVM model)
        {
            if (model == null || model.IdVacante <= 0)
                return Json(new { ok = false, message = "Modelo inválido" });

            using (var db = new SIGEPContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var vacante = db.Vacantes.FirstOrDefault(v => v.IdVacante == model.IdVacante);
                    if (vacante == null)
                        return Json(new { ok = false, message = "Vacante no encontrada" });

                    vacante.Nombre = model.Nombre;
                    vacante.IdEmpresa = model.IdEmpresa;
                    vacante.IdEstado = model.IdEstado;
                    vacante.Requerimientos = model.Requerimientos;
                    vacante.FechaMaxAplicacion = model.FechaMaxAplicacion;
                    vacante.NumCupos = model.NumCupos;
                    vacante.FechaCierre = model.FechaCierre;
                    vacante.IdModalidad = model.IdModalidad;
                    vacante.Descripcion = model.Descripcion;

                    db.SaveChanges();

                    // actualizar tabla intermedia: eliminar existentes y crear nueva si aplica
                    var existentes = db.EspecialidadesVacantes.Where(x => x.IdVacante == vacante.IdVacante).ToList();
                    if (existentes.Any())
                    {
                        db.EspecialidadesVacantes.RemoveRange(existentes);
                    }
                    if (model.IdEspecialidad > 0)
                    {
                        db.EspecialidadesVacantes.Add(new EspecialidadVacante
                        {
                            IdVacante = vacante.IdVacante,
                            IdEspecialidad = model.IdEspecialidad
                        });
                    }

                    db.SaveChanges();
                    tx.Commit();

                    return Json(new { ok = true, message = "Vacante actualizada correctamente" });
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
            using (var db = new SIGEPContext())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var especialidades = db.EspecialidadesVacantes.Where(ev => ev.IdVacante == id).ToList();
                    if (especialidades.Any())
                        db.EspecialidadesVacantes.RemoveRange(especialidades);

                    var postulaciones = db.PracticasEstudiantes.Where(p => p.IdVacante == id).ToList();
                    if (postulaciones.Any())
                        db.PracticasEstudiantes.RemoveRange(postulaciones);

                    var vacante = db.Vacantes.FirstOrDefault(v => v.IdVacante == id);
                    if (vacante != null)
                        db.Vacantes.Remove(vacante);

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
                             where p.IdVacante == idVacante
                             orderby u.Nombre
                             select new
                             {
                                 u.IdUsuario,
                                 u.Cedula,
                                 NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2
                             }).ToList();

                return Json(new { ok = true, data = lista }, JsonRequestBehavior.AllowGet);
            }
        }

        // ==============================
        // MÉTODOS PRIVADOS PARA DROPDOWNS
        // ==============================
        private List<SelectListItem> ObtenerEspecialidades()
        {
            using (var db = new SIGEPContext())
            {
                return db.Especialidades
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
            using (var db = new SIGEPContext())
            {
                return db.Modalidades
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
            using (var db = new SIGEPContext())
            {
                return db.Empresas
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
            using (var db = new SIGEPContext())
            {
                return db.Estados
                         .OrderBy(s => s.Descripcion)
                         .Select(s => new SelectListItem
                         {
                             Value = s.IdEstado.ToString(),
                             Text = s.Descripcion
                         }).ToList();
            }
        }
    }
}
