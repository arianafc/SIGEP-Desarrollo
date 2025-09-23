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
                // join a empresas y direcciones (left joins) + joins a estados, especialidades, modalidades
                var query = from v in db.VacantesPracticasTB
                            join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()

                                // join con DireccionesTB a través de e.IdDireccion
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
                                // <-- Ubicacion viene de DireccionesTB.DireccionExacta (vía EmpresasTB.IdDireccion)
                                Ubicacion = d != null ? d.DireccionExacta : "",
                                EstudiantesPostulados = db.PracticaEstudianteTB.Count(p => p.IdVacante == v.IdVacante)
                            };

                // filtros dinámicos
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
        // Nota: no almacenamos "Ubicacion" en la tabla VacantesPracticasTB porque la BD almacena la dirección en DireccionesTB,
        // vinculada desde EmpresasTB.IdDireccion. Si quieres permitir un "override" por vacante, habría que agregar una columna
        // a VacantesPracticasTB (no lo hacemos aquí).
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
                    var idEstado = model.IdEstado > 0 ? model.IdEstado : 1; // 1 = No Asignada (según tu catálogo)

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
                        // NO asignamos Ubicacion aquí (no existe esa columna en la tabla)
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
        [HttpGet]
        public JsonResult Detalle(int id)
        {
            using (var db = new SIGEPEntities())
            {
                var data = (from v in db.VacantesPracticasTB
                            join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                            from e in je.DefaultIfEmpty()

                                // join a direcciones para obtener DireccionExacta
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

                            where v.IdVacante == id
                            select new VacanteViewModel
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
                                EstadoNombre = es != null ? es.Descripcion : "",
                                // <-- tomamos la dirección exacta desde DireccionesTB
                                Ubicacion = d != null ? d.DireccionExacta : ""
                            }).FirstOrDefault();

                return Json(new { ok = data != null, data }, JsonRequestBehavior.AllowGet);
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

                    vacante.Nombre = model.Nombre;
                    vacante.IdEmpresa = model.IdEmpresa;
                    vacante.IdEstado = model.IdEstado;
                    vacante.Requerimientos = model.Requerimientos;
                    vacante.FechaMaxAplicacion = model.FechaMaxAplicacion;
                    vacante.NumCupos = model.NumCupos;
                    vacante.FechaCierre = model.FechaCierre;
                    vacante.IdModalidad = model.IdModalidad;
                    vacante.Descripcion = model.Descripcion;
                    // NO actualizamos Direcciones ni Ubicacion aquí — la dirección está en DireccionesTB vinculada por Empresa.IdDireccion

                    db.SaveChanges();

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
        // ASIGNAR ESTUDIANTE A VACANTE
        // ==============================
        [HttpPost]
        public JsonResult Asignar(int idVacante, int idUsuario)
        {
            using (var db = new SIGEPEntities())
            {
                var estadoAsignado = db.EstadosTB.FirstOrDefault(e => e.Descripcion == "Asignado");

                if (estadoAsignado == null)
                    return Json(new { ok = false, message = "El estado 'Asignado' no existe en EstadosTB" }, JsonRequestBehavior.AllowGet);

                var existente = db.PracticaEstudianteTB
                    .FirstOrDefault(p => p.IdVacante == idVacante && p.IdUsuario == idUsuario);

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

        public JsonResult GetUbicacionEmpresa(int idEmpresa)
        {
            using (var db = new SIGEPEntities())
            {
                var empresa = (from e in db.EmpresasTB
                               join d in db.DireccionesTB on e.IdDireccion equals d.IdDireccion
                               where e.IdEmpresa == idEmpresa
                               select new
                               {
                                   d.DireccionExacta
                               }).FirstOrDefault();

                if (empresa != null)
                {
                    return Json(new { ok = true, ubicacion = empresa.DireccionExacta }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { ok = false }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

