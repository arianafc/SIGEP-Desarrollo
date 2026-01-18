using Microsoft.Ajax.Utilities;
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
    [FiltroSesion]
    [ValidarUsuarioActivo]
    public class PracticasController : Controller
    {

        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();

        // ==============================
        // VISTA PRINCIPAL VACANTES
        // ==============================
        [FiltroCoordinador]
        [HttpGet]
        public ActionResult VacantesEstudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Modalidades = ObtenerModalidades();
            ViewBag.Empresas = ObtenerEmpresas();
            ViewBag.Estados = ObtenerEstados();
            ViewBag.Estados = ObtenerEstadosVacante();

            return View();
        }


        // ==============================
        // 🔹 GET VACANTES (LISTADO PRINCIPAL)
        // ==============================
        [HttpGet]
        public JsonResult GetVacantes(int idEstado = 0, int idEspecialidad = 0, int idModalidad = 0)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    // 🔹 Base de consulta
                    var vacantesQuery = db.VacantesPracticasTB
                        .AsNoTracking()
                        .Select(v => new
                        {
                            v.IdVacante,
                            v.Nombre,
                            v.IdEmpresa,
                            v.Requerimientos,
                            v.FechaMaxAplicacion,
                            v.NumCupos,
                            v.FechaCierre,
                            v.IdModalidad,
                            v.IdEstado,
                            v.Descripcion
                        });

                    // 🔹 Filtros opcionales
                    if (idEstado > 0)
                        vacantesQuery = vacantesQuery.Where(v => v.IdEstado == idEstado);

                    if (idModalidad > 0)
                        vacantesQuery = vacantesQuery.Where(v => v.IdModalidad == idModalidad);

                    var vacantes = vacantesQuery.ToList();

                    // 🔹 Filtro adicional por especialidad si aplica
                    if (idEspecialidad > 0)
                    {
                        var idsFiltrados = db.EspecialidadesVacantesTB
                            .Where(ev => ev.IdEspecialidad == idEspecialidad)
                            .Select(ev => ev.IdVacante)
                            .Distinct()
                            .ToList();

                        vacantes = vacantes.Where(v => idsFiltrados.Contains(v.IdVacante)).ToList();
                    }

                    // 🔹 Diccionarios de referencia
                    var empresas = db.EmpresasTB.ToDictionary(x => x.IdEmpresa, x => x.NombreEmpresa);
                    var modalidades = db.ModalidadesTB.ToDictionary(x => x.IdModalidad, x => x.Descripcion);
                    var estados = db.EstadosTB.ToDictionary(x => x.IdEstado, x => x.Descripcion);

                    var especialidadesPorVacante = db.EspecialidadesVacantesTB
                        .Join(db.EspecialidadesTB,
                              ev => ev.IdEspecialidad,
                              esp => esp.IdEspecialidad,
                              (ev, esp) => new { ev.IdVacante, esp.Nombre })
                        .GroupBy(x => x.IdVacante)
                        .ToDictionary(
                            g => g.Key,
                            g => string.Join(", ", g.Select(x => x.Nombre).Distinct())
                        );

                    // ================================
                    // 🔹 Cálculo de postulados válidos
                    // ================================
                    var estadosPostulados = new[] { 3, 5, 6, 8, 9, 11 }; // En proceso, Asignada, En curso, etc.

                    var postuladosPorVacante = (
                        from p in db.PracticaEstudianteTB
                        join ue in db.UsuarioEspecialidadTB on p.IdUsuario equals ue.IdUsuario
                        join ev in db.EspecialidadesVacantesTB on p.IdVacante equals ev.IdVacante
                        where estadosPostulados.Contains(p.IdEstado)
                              && ue.IdEspecialidad == ev.IdEspecialidad
                              && ue.IdEstado == 1 // solo especialidades activas
                        group p by p.IdVacante into g
                        select new
                        {
                            IdVacante = g.Key,
                            Conteo = g.Select(x => x.IdUsuario).Distinct().Count()
                        }
                    ).ToDictionary(x => x.IdVacante, x => x.Conteo);

                    // ================================
                    // 🔹 Cálculo de cupos ocupados
                    // ================================
                    var estadosOcupandoCupo = new[] { 5, 6, 8, 9, 11 };

                    var cuposOcupadosPorVacante = db.PracticaEstudianteTB
                        .Where(p => estadosOcupandoCupo.Contains(p.IdEstado))
                        .GroupBy(p => p.IdVacante)
                        .ToDictionary(g => g.Key, g => g.Count());

                    // ================================
                    // 🔹 Resultado Final
                    // ================================
                    var result = vacantes
                        .Select(v => new
                        {
                            v.IdVacante,
                            v.Nombre,
                            v.IdEmpresa,
                            EmpresaNombre = empresas.ContainsKey(v.IdEmpresa) ? empresas[v.IdEmpresa] : "—",
                            v.Requerimientos,
                            FechaMaxAplicacion = v.FechaMaxAplicacion?.ToString("yyyy-MM-dd"),
                            NumCupos = v.NumCupos ?? 0,
                            FechaCierre = v.FechaCierre?.ToString("yyyy-MM-dd"),
                            v.IdModalidad,
                            ModalidadNombre = modalidades.ContainsKey(v.IdModalidad ?? 0)
                                ? modalidades[v.IdModalidad ?? 0]
                                : "—",
                            v.Descripcion,
                            v.IdEstado,
                            EstadoNombre = estados.ContainsKey(v.IdEstado) ? estados[v.IdEstado] : "—",
                            EspecialidadNombre = especialidadesPorVacante.ContainsKey(v.IdVacante)
                                ? especialidadesPorVacante[v.IdVacante]
                                : "—",
                            NumPostulados = postuladosPorVacante.ContainsKey(v.IdVacante)
                                ? postuladosPorVacante[v.IdVacante]
                                : 0,
                            CuposOcupados = cuposOcupadosPorVacante.ContainsKey(v.IdVacante)
                                ? cuposOcupadosPorVacante[v.IdVacante]
                                : 0
                        })
                        .OrderByDescending(v => v.IdVacante)
                        .ToList();

                    return Json(new { ok = true, data = result }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = new object[0], message = ex.Message },
                            JsonRequestBehavior.AllowGet);
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
                    var idEstado = model.IdEstado > 0 ? model.IdEstado : 1;
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
                         .AsEnumerable()
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

        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var vacante = db.VacantesPracticasTB.FirstOrDefault(v => v.IdVacante == id);
                    if (vacante == null)
                        return Json(new { ok = false, message = "La vacante no existe." });

                    // ==========================================
                    // DEFINICIÓN DE ESTADOS
                    // ==========================================
                    int idArchivado = 10;

                    // Estados que representan procesos activos
                    var estadosActivos = new int[] { 3, 5, 6, 11, 12 }; // Ejemplo: En proceso, Asignada, En curso, etc.

                    // Estados que permiten archivar si todos los estudiantes están así
                    var estadosFinalizados = new int[] { 8, 9 }; // Ejemplo: Finalizada (8), Rezagada (9)
                    var estadosRechRet = new int[] { 4, 7 };     // Ejemplo: Rechazada (4), Retirada (7)

                    // ==========================================
                    // VALIDACIONES
                    // ==========================================
                    var relaciones = db.PracticaEstudianteTB
                        .Where(pe => pe.IdVacante == id)
                        .Select(pe => pe.IdEstado)
                        .ToList();

                    // Caso 1: No hay estudiantes asociados
                    if (relaciones.Count == 0)
                    {
                        vacante.IdEstado = idArchivado;
                        db.SaveChanges();
                        return Json(new { ok = true, message = "Vacante archivada correctamente (sin estudiantes asociados)." });
                    }

                    // Caso 2: Si hay estudiantes, verificar si todos están en estados que permiten archivar
                    bool todosFinalizados =
                        relaciones.All(s => estadosFinalizados.Contains(s) || estadosRechRet.Contains(s));

                    bool hayActivos = relaciones.Any(s => estadosActivos.Contains(s));

                    if (hayActivos)
                    {
                        return Json(new
                        {
                            ok = false,
                            message = "No se puede archivar: existen estudiantes con procesos activos."
                        });
                    }

                    if (!todosFinalizados)
                    {
                        return Json(new
                        {
                            ok = false,
                            message = "No se puede archivar: algunos estudiantes no están en estado finalizado, rezagado, rechazada o retirada."
                        });
                    }

                    // ==========================================
                    // SI PASA VALIDACIÓN → ARCHIVAR
                    // ==========================================
                    foreach (var pe in db.PracticaEstudianteTB.Where(p => p.IdVacante == id))
                    {
                        pe.IdEstado = idArchivado;
                        pe.FechaAplicacion = DateTime.Now;
                    }

                    vacante.IdEstado = idArchivado;

                    // AUDITORÍA
                    var idUsuarioSesion = Session["IdUsuario"] as int?;
                    if (idUsuarioSesion != null)
                    {
                        db.AuditoriaGlobalTB.Add(new AuditoriaGlobalTB
                        {
                            IdUsuario = idUsuarioSesion.Value,
                            TablaAfectada = "VacantesPracticasTB",
                            IdRegistro = vacante.IdVacante,
                            Accion = "Archivar (Desactivar práctica)",
                            CampoAfectado = "IdEstado",
                            DatosAnteriores = vacante.IdEstado.ToString(),
                            DatosNuevos = idArchivado.ToString(),
                        });
                    }

                    db.SaveChanges();

                    return Json(new { ok = true, message = "Vacante archivada correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al archivar la vacante: " + ex.Message });
            }
        }



        // ==============================
        // 🔹 OBTENER POSTULACIONES (MODAL "VER MÁS")
        // ==============================
        [HttpGet]
        public JsonResult ObtenerPostulaciones(int idVacante)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var lista = (from p in db.PracticaEstudianteTB
                                 join u in db.UsuariosTB on p.IdUsuario equals u.IdUsuario
                                 join e in db.EstadosTB on p.IdEstado equals e.IdEstado
                                 join v in db.VacantesPracticasTB on p.IdVacante equals v.IdVacante
                                 join emp in db.EmpresasTB on v.IdEmpresa equals emp.IdEmpresa
                                 join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
                                 join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante
                                 where p.IdVacante == idVacante
                                       && ue.IdEspecialidad == ev.IdEspecialidad
                                       && ue.IdEstado == 1   // solo especialidades activas
                                 orderby u.Nombre
                                 select new
                                 {
                                     p.IdPractica,
                                     p.IdVacante,
                                     u.IdUsuario,
                                     u.Cedula,
                                     NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                                     p.IdEstado,
                                     EstadoDescripcion = e.Descripcion,
                                     Empresa = emp.NombreEmpresa
                                 }).Distinct().ToList();

                    return Json(new { ok = true, data = lista }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, data = new object[0], message = ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
        }



        [HttpGet]
        public JsonResult ObtenerEstudiantesAsignar(int idVacante)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                    return Json(new { ok = false, message = "Sesión expirada." }, JsonRequestBehavior.AllowGet);

                int idUsuarioSesion = Convert.ToInt32(Session["IdUsuario"]);

                using (var db = new SIGEPEntities())
                {
                    var data = db.Database.SqlQuery<EstudianteAsignarDTO>(
                        "EXEC ObtenerEstudiantesAsignarSP @IdVacante, @IdUsuarioSesion",
                        new SqlParameter("@IdVacante", idVacante),
                        new SqlParameter("@IdUsuarioSesion", idUsuarioSesion)
                    ).ToList();

                    return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public class EstudianteAsignarDTO
        {
            public int IdUsuario { get; set; }
            public string Cedula { get; set; }
            public string NombreCompleto { get; set; }
            public string Especialidad { get; set; }
            public bool EstadoAcademico { get; set; }
            public string EstadoPractica { get; set; }
            public bool TienePracticaActiva { get; set; }
            public string EstadoVacante { get; set; }
            public bool TieneRelacionEnVacante { get; set; }
            public int IdPracticaVacante { get; set; }
        }

        [HttpGet]
        public JsonResult ObtenerVacantesAsignar(int idUsuario)
        {
            using (var db = new SIGEPEntities())
            {
                try
                {

                    if (Session["IdUsuario"] == null)
                        return Json(new { ok = false, message = "Sesión expirada." }, JsonRequestBehavior.AllowGet);

                    var result = db.Database.SqlQuery<VacanteDisponibleDTO>(
                        "EXEC ObtenerVacantesAsignarSP @IdUsuario",
                        new SqlParameter("@IdUsuario", idUsuario)
                    ).ToList();

                    return Json(new { ok = true, data = result }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { ok = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
        }


        // DTO para mapear resultados
        public class VacanteDisponibleDTO
        {
            public int IdVacante { get; set; }
            public string NombreVacante { get; set; }
            public string NombreEmpresa { get; set; }
            public string Especialidad { get; set; }
            public int NumCupos { get; set; }
            public int CuposOcupados { get; set; }
            public DateTime? FechaCierre { get; set; }
            public string Requerimientos { get; set; }
            public string Tipo { get; set; }
            public string EstadoPractica { get; set; }
            public int PuedeAsignar { get; set; }
            public int IdPracticaVacante { get; set; }    
            public string NombreCompleto { get; set; } 
            public string EstadoAcademicoDescripcion { get; set; }  
        }


        [HttpPost]
        public JsonResult AsignarEstudiante(int idVacante, int idUsuario)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var result = db.Database.SqlQuery<AsignarEstudianteResult>(
                        "EXEC AsignarEstudianteSP @IdVacante, @IdUsuario",
                        new SqlParameter("@IdVacante", idVacante),
                        new SqlParameter("@IdUsuario", idUsuario)
                    ).FirstOrDefault();

                    if (result == null)
                    {
                        return Json(new
                        {
                            ok = false,
                            message = "No se obtuvo respuesta del procedimiento almacenado."
                        });
                    }

                    bool ok = result.ok == 1;
                    string msg = result.message ?? (ok
                        ? "Operación realizada correctamente."
                        : "No se pudo completar la asignación.");

                    return Json(new { ok, message = msg });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al asignar estudiante: " + ex.Message });
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

        [HttpPost]
        public JsonResult RetirarEstudiante(int idVacante, int idUsuario)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var estadoRetirada = db.EstadosTB
                        .FirstOrDefault(e => e.Descripcion.Trim().ToLower() == "retirada");

                    if (estadoRetirada == null)
                        return Json(new { ok = false, message = "El estado 'Retirada' no existe en EstadosTB." });

                    // Siempre trabajamos con el ÚLTIMO registro de esa vacante y estudiante
                    var practica = db.PracticaEstudianteTB
                        .Include("EstadosTB")
                        .Where(p => p.IdVacante == idVacante && p.IdUsuario == idUsuario)
                        .OrderByDescending(p => p.IdPractica)
                        .FirstOrDefault();

                    if (practica == null)
                    {
                        return Json(new
                        {
                            ok = false,
                            message = "No se encontró práctica asociada a este estudiante para esta vacante."
                        });
                    }

                    var estadoActual = (practica.EstadosTB?.Descripcion ?? "").Trim().ToLower();

                    if (estadoActual != "asignada" && estadoActual != "en proceso de aplicacion")
                    {
                        return Json(new
                        {
                            ok = false,
                            message = $"No se puede retirar una práctica en estado '{practica.EstadosTB.Descripcion}'."
                        });
                    }

                    practica.IdEstado = estadoRetirada.IdEstado;
                    practica.FechaAplicacion = DateTime.Now;
                    db.SaveChanges();

                    return Json(new { ok = true, message = "El estudiante ha sido marcado como 'Retirada'." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al retirar estudiante: " + ex.Message });
            }
        }


        // ==============================
        // DESASIGNAR ESTUDIANTE
        // ==============================
        [HttpPost]
        public JsonResult DesasignarEstudiante(int idUsuario, int idVacante)
        {
            try
            {
                if (Session["IdRol"] == null)
                    return Json(new { ok = false, mensaje = "Sesión expirada. Por favor inicie sesión nuevamente." }, JsonRequestBehavior.AllowGet);

                int rol = Convert.ToInt32(Session["IdRol"]);
                // Solo Profesor (3) o Coordinador (2) pueden desasignar
                if (rol != 2 && rol != 3)
                    return Json(new { ok = false, mensaje = "No tiene permisos para desasignar estudiantes." }, JsonRequestBehavior.AllowGet);

                using (var db = new SIGEPEntities())
                {
                    var practica = db.PracticaEstudianteTB
                        .Include("EstadosTB")
                        .FirstOrDefault(p => p.IdUsuario == idUsuario && p.IdVacante == idVacante);

                    if (practica == null)
                        return Json(new { ok = false, mensaje = "No se encontró la práctica del estudiante." }, JsonRequestBehavior.AllowGet);

                    string estadoActual = practica.EstadosTB?.Descripcion?.Trim().ToLower() ?? "";


                    if (estadoActual != "en proceso de aplicacion" && estadoActual != "asignada")
                    {
                        return Json(new
                        {
                            ok = false,
                            mensaje = $"No se puede desasignar una práctica en estado '{estadoActual}'. Solo 'En proceso de Aplicación' o 'Asignada'."
                        }, JsonRequestBehavior.AllowGet);
                    }


                    var estadoRetirada = db.EstadosTB.FirstOrDefault(e => e.Descripcion.Trim().ToLower() == "retirada");
                    if (estadoRetirada == null)
                        return Json(new { ok = false, mensaje = "No existe el estado 'Retirada' en la BD." }, JsonRequestBehavior.AllowGet);


                    practica.IdEstado = estadoRetirada.IdEstado;
                    practica.FechaAplicacion = DateTime.Now;
                    db.SaveChanges();


                    if (Session["IdUsuario"] != null)
                    {
                        int idUsuarioSesion = Convert.ToInt32(Session["IdUsuario"]);
                        db.AuditoriaGlobalTB.Add(new AuditoriaGlobalTB
                        {
                            IdUsuario = idUsuarioSesion,
                            TablaAfectada = "PracticaEstudianteTB",
                            IdRegistro = practica.IdPractica,
                            Accion = "Desasignar Estudiante",
                            CampoAfectado = "IdEstado",
                            DatosAnteriores = estadoActual,
                            DatosNuevos = "retirada"
                        });
                        db.SaveChanges();
                    }

                    return Json(new { ok = true, mensaje = "Estudiante desasignado correctamente (estado: Retirada)." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, mensaje = "Error al desasignar: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerEstudiantesPracticas(int idUsuarioSesion)
        {
            try
            {
                var data = db.Database.SqlQuery<EstudiantePracticaVM>(
                    "EXEC ObtenerEstudiantesPracticasSP @IdUsuarioSesion",
                    new SqlParameter("@IdUsuarioSesion", idUsuarioSesion)
                ).ToList();

                return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
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
                var empresas = db.Database.SqlQuery<EmpresaDTO>(
                    "EXEC ObtenerEmpresasActivasSP"
                ).ToList();

                return empresas.Select(e => new SelectListItem
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

        private List<SelectListItem> ObtenerEstadosVacante()
        {
            using (var db = new SIGEPEntities())
            {
                var estados = db.EstadosTB
                    .Where(s => s.Descripcion == "Activo" || s.Descripcion == "Archivado")
                    .OrderBy(s => s.Descripcion)
                    .Select(s => new SelectListItem
                    {
                        Value = s.IdEstado.ToString(),
                        Text = s.Descripcion
                    })
                    .ToList();

                return estados;
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

                var ubicacion = (from e in db.EmpresasTB
                                 join d in db.DireccionesTB on e.IdDireccion equals d.IdDireccion into jd
                                 from d in jd.DefaultIfEmpty()
                                 where e.IdEmpresa == idEmpresa
                                 select d.DireccionExacta).FirstOrDefault();

                return Json(new { ok = ubicacion != null, ubicacion = ubicacion ?? "" }, JsonRequestBehavior.AllowGet);
            }
        }

        [FiltroProfesor]
        [HttpGet]
        public ActionResult VistaVacantesProfesor()
        {
            try
            {
                
                if (Session["IdUsuario"] == null || Session["IdRol"] == null)
                    return RedirectToAction("Login", "Home");

                // Validar que el rol sea profesor (IdRol == 3)
                if (!int.TryParse(Session["IdRol"].ToString(), out int idRol) || idRol != 3)
                    return RedirectToAction("Login", "Home"); 

                using (var db = new SIGEPEntities())
                {
                    var vacantes = db.VacantesPracticasTB
                        .Include("EmpresasTB")
                        .Include("ModalidadesTB")
                        .Include("EstadosTB")
                        .ToList();

                    var usuarios = db.UsuariosTB
                        .Select(u => new SelectListItem
                        {
                            Value = u.IdUsuario.ToString(),
                            Text = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2
                        }).ToList();

                    ViewBag.Usuarios = usuarios;
                    ViewBag.Modalidades = ObtenerModalidades();
                    ViewBag.Especialidades = ObtenerEspecialidades();
                    ViewBag.Estados = ObtenerEstadosVacante();

                    return View(vacantes);
                }
            }
            catch
            {
                
                return RedirectToAction("Login", "Home");
            }
        }

        [HttpGet]
        public ActionResult VisualizacionPostulacion(int idVacante, int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var datosPractica = dbContext.ObtenerVisualizacionPracticaSP(idVacante, idUsuario).FirstOrDefault();

                    if (datosPractica == null)
                    {
                        ViewBag.Error = "No se encontró información de la práctica.";
                        return View(new VacantePracticaVM());
                    }

                    var estadosPermitidos = new List<string> {
                "En Proceso de Aplicacion",
                "Rechazada",
                "Asignada",
                "Retirada",
                "En Curso"
            };

                    var viewModel = new VacantePracticaVM
                    {
                        IdVacante = datosPractica.IdVacante,
                        Nombre = datosPractica.Nombre,
                        EmpresaNombre = datosPractica.EmpresaNombre,
                        Requerimientos = datosPractica.Requerimientos,
                        FechaMaxAplicacion = datosPractica.FechaMaxAplicacion,
                        ModalidadNombre = datosPractica.ModalidadNombre,
                        IdUsuario = datosPractica.IdUsuario,
                        EstudianteNombre = datosPractica.EstudianteNombre,
                        EstudianteCedula = datosPractica.EstudianteCedula,
                        EstudianteCorreo = datosPractica.EstudianteCorreo,
                        EstudianteEdad = datosPractica.EstudianteEdad,
                        EstudianteEspecialidad = datosPractica.EstudianteEspecialidad,
                        ContactoEmpresaNombre = datosPractica.ContactoEmpresaNombre,
                        ContactoEmpresaEmail = datosPractica.ContactoEmpresaEmail,
                        ContactoEmpresaTelefono = datosPractica.ContactoEmpresaTelefono,
                        IdPractica = datosPractica.IdPractica,
                        FechaAplicacion = datosPractica.FechaAplicacion,
                        EstadoPractica = datosPractica.EstadoPractica,
                        Nota1 = datosPractica.Nota1,
                        Nota2 = datosPractica.Nota2,
                        NotaFinal = datosPractica.NotaFinal,
                        ListaEstados = dbContext.EstadosTB
                            .Where(e => estadosPermitidos.Contains(e.Descripcion))
                            .OrderBy(e => e.Descripcion)
                            .Select(e => new EstadoVM
                            {
                                IdEstado = e.IdEstado,
                                Descripcion = e.Descripcion
                            }).ToList()
                    };

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

        [HttpGet]
        public ActionResult VisualizacionPorPractica(int idPractica)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var practica = dbContext.PracticaEstudianteTB
                        .Where(p => p.IdPractica == idPractica)
                        .Select(p => new
                        {
                            p.IdVacante,
                            p.IdUsuario
                        })
                        .FirstOrDefault();

                    if (practica == null)
                    {
                        TempData["SwalError"] = "No se encontró información de la práctica seleccionada.";
                        return RedirectToAction("PracticasCoordinador");
                    }

                    return RedirectToAction("VisualizacionPostulacion", new
                    {
                        idVacante = practica.IdVacante,
                        idUsuario = practica.IdUsuario
                    });
                }
            }
            catch (Exception)
            {
                TempData["SwalError"] = "Ocurrió un error al intentar cargar la práctica.";
                return RedirectToAction("PracticasCoordinador");
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
                    int idUsuarioSesion = Convert.ToInt32(Session["IdUsuario"]);

                    // Obtener la práctica
                    var practica = dbContext.PracticaEstudianteTB.FirstOrDefault(p => p.IdPractica == idPractica);

                    if (practica == null)
                    {
                        return Json(new { success = false, message = "Práctica no encontrada" });
                    }

                    // VALIDACIÓN 1: Verificar estado académico del estudiante
                    var estudiante = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == practica.IdUsuario);

                    if (estudiante != null && estudiante.EstadoAcademico == false)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "El estudiante no puede realizar ninguna práctica debido a que su estado académico es Rezagado. Por favor, contacte al coordinador académico."
                        });
                    }

                    // VALIDACIÓN 2: Si se intenta asignar, verificar que no tenga otra práctica activa
                    var estadoNuevo = dbContext.EstadosTB.FirstOrDefault(e => e.IdEstado == idEstado);

                    if (estadoNuevo != null && estadoNuevo.Descripcion == "Asignada")
                    {
                        // Verificar si ya tiene una práctica en estados conflictivos
                        var estadosConflictivos = new[] { "Asignada", "En Curso", "Aprobada", "Finalizada" };

                        var practicaExistente = dbContext.PracticaEstudianteTB
                            .Where(p => p.IdUsuario == practica.IdUsuario &&
                                        p.IdPractica != idPractica)
                            .Join(dbContext.EstadosTB,
                                  p => p.IdEstado,
                                  e => e.IdEstado,
                                  (p, e) => new { Practica = p, Estado = e })
                            .FirstOrDefault(x => estadosConflictivos.Contains(x.Estado.Descripcion));

                        if (practicaExistente != null)
                        {
                            var vacante = dbContext.VacantesPracticasTB
                                .FirstOrDefault(v => v.IdVacante == practicaExistente.Practica.IdVacante);

                            return Json(new
                            {
                                success = false,
                                message = $"El estudiante ya tiene una práctica en estado '{practicaExistente.Estado.Descripcion}' ({vacante?.Nombre ?? "Sin nombre"}). Si desea asignar otra práctica, primero debe retirar o finalizar la práctica actual."
                            });
                        }
                    }

                    // Ejecutar el SP para actualizar estado
                    var resultado = dbContext.ActualizarEstadoPracticaSP(
                        idPractica,
                        idEstado,
                        comentario,
                        idUsuarioSesion
                    ).FirstOrDefault();

                    if (resultado == null)
                    {
                        return Json(new { success = false, message = "No se encontró la práctica." });
                    }

                    // Enviar correo de notificación
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

        [FiltroUsuarioAdmin]
        [HttpGet]
        public ActionResult PracticasCoordinador()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Home");
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Modalidades = ObtenerModalidades();
            ViewBag.Estados = ObtenerEstados();

            return View();
        }

        [HttpGet]
        public JsonResult GetVacantesProfesor(string estado = "", int idModalidad = 0, int idEspecialidad = 0)
        {
            using (var db = new SIGEPEntities())
            {
                if (Session["IdUsuario"] == null)
                    return Json(new { ok = false, message = "Sesión expirada." }, JsonRequestBehavior.AllowGet);

                int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

                // 1️⃣ Especialidad(es) del profesor
                List<int> especialidadesProfesor = new List<int>();
                if (Session["IdEspecialidad"] != null)
                {
                    especialidadesProfesor.Add(Convert.ToInt32(Session["IdEspecialidad"]));
                }
                else
                {
                    especialidadesProfesor = db.UsuarioEspecialidadTB
                        .Where(ue => ue.IdUsuario == idProfesor && ue.IdEstado == 1)
                        .Select(ue => ue.IdEspecialidad)
                        .ToList();
                }

                if (!especialidadesProfesor.Any())
                    return Json(new { ok = true, data = new object[0] }, JsonRequestBehavior.AllowGet);

                string estadoNorm = (estado ?? "").Trim().ToLower();
                string autog = "autogestionada";

                
                var estadosOcupanCupo = new[] { 5, 6, 8, 9, 11 };
                
                var q =
                    from v in db.VacantesPracticasTB

                    join e in db.EmpresasTB on v.IdEmpresa equals e.IdEmpresa into je
                    from e in je.DefaultIfEmpty()

                    join es in db.EstadosTB on v.IdEstado equals es.IdEstado

                    join ev in db.EspecialidadesVacantesTB on v.IdVacante equals ev.IdVacante
                    join sp in db.EspecialidadesTB on ev.IdEspecialidad equals sp.IdEspecialidad

                    join m in db.ModalidadesTB on v.IdModalidad equals m.IdModalidad into jm
                    from m in jm.DefaultIfEmpty()

                    where especialidadesProfesor.Contains(ev.IdEspecialidad)
                    select new
                    {
                        v.IdVacante,
                        v.Nombre,
                        v.IdEmpresa,
                        EmpresaNombre = e != null ? e.NombreEmpresa : "",
                        v.Requerimientos,
                        v.FechaMaxAplicacion,
                        v.NumCupos,
                        v.FechaCierre,
                        v.Descripcion,
                        IdModalidad = v.IdModalidad ?? 0,
                        ModalidadNombre = m != null ? m.Descripcion : "",
                        IdEspecialidad = ev.IdEspecialidad,
                        EspecialidadNombre = sp != null ? sp.Nombre : "",
                        v.IdEstado,
                        EstadoNombre = es != null ? es.Descripcion : "",
                        TipoVacante = v.Tipo,

                        // 🔹 CONTEO CORRECTO: Solo estados que ocupan cupo
                        // Excluye: En proceso de Aplicación (3), Rechazada (4), Retirada (7)
                        EstudiantesPostulados = (
                            from p in db.PracticaEstudianteTB
                            join ue in db.UsuarioEspecialidadTB on p.IdUsuario equals ue.IdUsuario
                            where p.IdVacante == v.IdVacante
                               && estadosOcupanCupo.Contains(p.IdEstado)  // 🔥 Solo estados activos
                               && ue.IdEspecialidad == ev.IdEspecialidad
                               && ue.IdEstado == 1  // Especialidad activa
                            select p.IdUsuario
                        ).Distinct().Count()
                    };

                // 4️⃣ Excluir Autogestionadas
                q = q.Where(x =>
                    ((x.EstadoNombre ?? "").ToLower() != autog) &&
                    ((x.TipoVacante ?? "").ToLower() != autog)
                );

                // 5️⃣ Filtros de UI
                if (!string.IsNullOrEmpty(estadoNorm))
                    q = q.Where(x => (x.EstadoNombre ?? "").ToLower() == estadoNorm);

                if (idModalidad > 0)
                    q = q.Where(x => x.IdModalidad == idModalidad);

                if (idEspecialidad > 0)
                    q = q.Where(x => x.IdEspecialidad == idEspecialidad);

                // 6️⃣ Materializar + formatear fechas
                var list = q.OrderByDescending(x => x.IdVacante).ToList();

                var data = list.Select(x => new
                {
                    x.IdVacante,
                    x.Nombre,
                    x.IdEmpresa,
                    x.EmpresaNombre,
                    x.Requerimientos,
                    FechaMaxAplicacion = x.FechaMaxAplicacion.HasValue
                        ? x.FechaMaxAplicacion.Value.ToString("o")
                        : null,
                    NumCupos = x.NumCupos ?? 0,
                    FechaCierre = x.FechaCierre.HasValue
                        ? x.FechaCierre.Value.ToString("o")
                        : null,
                    x.Descripcion,
                    x.IdModalidad,
                    x.ModalidadNombre,
                    x.IdEspecialidad,
                    x.EspecialidadNombre,
                    x.IdEstado,
                    x.EstadoNombre,
                    x.EstudiantesPostulados  // ✅ Ahora excluye "En proceso de Aplicación"
                });

                return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [FiltroEstudiante]
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
                // Lista de nombres de estados permitidos
                var estadosPermitidos = new List<string> { "Asignada", "Aprobada", "Finalizada", "En Curso" };

                var practica = dbContext.PracticaEstudianteTB
                    .Where(p => p.IdUsuario == idUsuario)
                    .Join(dbContext.EstadosTB,
                          p => p.IdEstado,
                          e => e.IdEstado,
                          (p, e) => new { Practica = p, Estado = e })
                    .Where(x => estadosPermitidos.Contains(x.Estado.Descripcion))
                    .OrderByDescending(x => x.Practica.FechaAplicacion)
                    .Select(x => x.Practica)
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

        [FiltroEstudiante]
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
                    // Obtener el estado académico del estudiante
                    var usuario = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);
                    var estadoAcademico = usuario?.EstadoAcademico ?? true; // true por defecto (Aprobado)

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
                        Postulaciones = postulaciones,
                        EstadoAcademico = estadoAcademico
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

        [FiltroUsuarioAdmin]
        [HttpGet]
        public ActionResult ListadoEstudiantes()
        {
            if (Session["IdUsuario"] == null)
                return RedirectToAction("Login", "Home");

            return View();
        }



        [HttpGet]
        public JsonResult ListarEstudiantesJson(int? idVacante = null)
        {
            if (Session["IdUsuario"] == null)
                return Json(new { data = new object[0] }, JsonRequestBehavior.AllowGet);

            int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
            int idRol = Session["IdRol"] != null ? Convert.ToInt32(Session["IdRol"]) : 0;

            using (var db = new SIGEPEntities())
            {
                if (idRol == 2 || idRol == 3)
                {
                    int anioActual = DateTime.Now.Year;

                    var especialidadesProfesor = new List<int>();
                    if (idRol == 3)
                    {
                        especialidadesProfesor = db.UsuarioEspecialidadTB
                            .Where(ue => ue.IdUsuario == idUsuario && ue.IdEstado == 1)
                            .Select(ue => ue.IdEspecialidad)
                            .ToList();
                    }

                    var query =
                        from p in db.PracticaEstudianteTB
                        join u in db.UsuariosTB on p.IdUsuario equals u.IdUsuario
                        join v in db.VacantesPracticasTB on p.IdVacante equals v.IdVacante
                        join emp in db.EmpresasTB on v.IdEmpresa equals emp.IdEmpresa
                        join est in db.EstadosTB on p.IdEstado equals est.IdEstado
                        join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario
                        join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                        where ue.IdEstado == 1
        && u.IdRol == 1
        && p.FechaAplicacion.Year == anioActual
                        select new { p, u, v, emp, est, ue, esp };

                    if (idRol == 3 && especialidadesProfesor.Any())
                    {
                        query = query.Where(x => especialidadesProfesor.Contains(x.ue.IdEspecialidad));
                    }

                    if (idRol == 3 && idVacante.HasValue && idVacante.Value > 0)
                    {
                        int idVac = idVacante.Value;
                        query = query.Where(x => x.v.IdVacante == idVac);
                    }

                    var datos = query
                        .ToList()
                        .GroupBy(x => x.p.IdPractica)
                        .Select(g => g.First())
                        .Select(x => new
                        {
                            IdUsuario = x.u.IdUsuario,
                            Cedula = x.u.Cedula,
                            Nombre = (x.u.Nombre + " " + x.u.Apellido1 + " " + x.u.Apellido2).Trim(),
                            Especialidad = x.esp.Nombre,
                            Telefono = db.TelefonosTB
                                .Where(t => t.IdUsuario == x.u.IdUsuario)
                                .Select(t => t.Telefono)
                                .FirstOrDefault(),
                            EstadoPostulacion = x.est.Descripcion,
                            Empresa = x.emp.NombreEmpresa,
                            Tipo = x.v.Tipo ?? "—",
                            IdPracticaVacante = x.p.IdPractica,
                            EstadoVacante = x.est.Descripcion,
                            IdVacanteUltima = (int?)x.v.IdVacante,
                            TipoMensaje = ""
                        })
                        .OrderBy(x => x.Nombre)
                        .ToList();

                    return Json(new { data = datos }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { data = new object[0] }, JsonRequestBehavior.AllowGet);
            }
        }

        private static readonly string[] EstadosActivos = new[]
        {
            "En Proceso de Aplicacion", "En Proceso de Aplicación",
            "En Curso",
            "Asignada",
            "Pendiente de Aprobación", "Pendiente de Aprobacion",
            "Aprobada"
        };

        private string ClasificarEstadoPostulacion(int idUsuario, out int? idPracticaActiva, out string estadoVacante)
        {
            idPracticaActiva = null;
            estadoVacante = null;


            var practicas = db.PracticaEstudianteTB
                .Include("EstadosTB")
                .Where(p => p.IdUsuario == idUsuario)
                .OrderByDescending(p => p.FechaAplicacion)
                .ThenByDescending(p => p.IdPractica)
                .ToList();


            if (!practicas.Any())
                return "Sin proceso activo";


            var ultima = practicas.First();
            var desc = (ultima.EstadosTB?.Descripcion ?? "").Trim();
            estadoVacante = desc;
            idPracticaActiva = ultima.IdPractica;


            if (string.IsNullOrEmpty(desc))
                return "Sin proceso activo";

            return desc;
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
            var ultimaVacante = db.PracticaEstudianteTB
                .Where(p => p.IdUsuario == idUsuario && p.IdVacante != null)
                .OrderByDescending(p => p.FechaAplicacion)
                .ThenByDescending(p => p.IdPractica)
                .Select(p => (int?)p.IdVacante)
                .FirstOrDefault();

            return ultimaVacante;
        }



        [HttpPost]

        public JsonResult DesasignarPractica(int idPractica, string comentario)
        {
            try
            {
                if (idPractica <= 0)
                    return Json(new { ok = false, msg = "Id de práctica inválido." });

                if (Session["IdRol"] == null)
                    return Json(new { ok = false, msg = "Sesión expirada. Inicie sesión nuevamente." });

                int rol = Convert.ToInt32(Session["IdRol"]);
                if (rol != 2 && rol != 3)
                    return Json(new { ok = false, msg = "No tiene permisos para desasignar estudiantes." });

                int idUsuarioSesion = Session["IdUsuario"] != null
                    ? Convert.ToInt32(Session["IdUsuario"])
                    : 0;

                using (var db = new SIGEPEntities())
                {
                    var practica = db.PracticaEstudianteTB
                        .Include("EstadosTB")
                        .FirstOrDefault(p => p.IdPractica == idPractica);

                    if (practica == null)
                        return Json(new { ok = false, msg = "No se encontró la práctica del estudiante." });

                    var desc = (practica.EstadosTB?.Descripcion ?? "").Trim().ToLower();

                 
                    desc = desc
                        .Replace("á", "a")
                        .Replace("é", "e")
                        .Replace("í", "i")
                        .Replace("ó", "o")
                        .Replace("ú", "u");

                    if (desc != "asignada" && desc != "en proceso de aplicacion")
                    {
                        return Json(new
                        {
                            ok = false,
                            msg = $"No se puede desasignar una práctica en estado '{practica.EstadosTB?.Descripcion}'. Solo 'Asignada' o 'En proceso de aplicación'."
                        });
                    }

                    int idEstado = db.EstadosTB
                         .Where(e => e.Descripcion.Trim().ToLower() == "retirada")
                         .Select(e => e.IdEstado)
                         .FirstOrDefault();

                    if (idEstado == 0)
                        return Json(new { ok = false, msg = "No se encontró el estado 'Retirada'." });

                    db.Database.ExecuteSqlCommand(
                        "EXEC dbo.ActualizarEstadoPracticaSP @IdPractica, @IdEstado, @Comentario, @IdUsuarioSesion",
                        new SqlParameter("@IdPractica", idPractica),
                        new SqlParameter("@IdEstado", idEstado),
                        new SqlParameter("@Comentario", (object)comentario ?? DBNull.Value),
                        new SqlParameter("@IdUsuarioSesion", idUsuarioSesion)
                    );

                    db.AuditoriaGlobalTB.Add(new AuditoriaGlobalTB
                    {
                        IdUsuario = idUsuarioSesion,
                        TablaAfectada = "PracticaEstudianteTB",
                        IdRegistro = practica.IdPractica,
                        Accion = "Desasignar (Retirada)",
                        CampoAfectado = "IdEstado",
                        DatosAnteriores = practica.EstadosTB?.Descripcion,
                        DatosNuevos = "retirada"
                    });
                    db.SaveChanges();

                    return Json(new { ok = true, msg = "✅ Estudiante desasignado correctamente (estado: Retirada)." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = "❌ Error al desasignar: " + ex.Message });
            }
        }


        // Cambiar estado académico del usuario
        [HttpPost]
        public JsonResult CambiarEstadoAcademico(int idUsuario, string nuevoEstado)
        {
            try
            {
                int idEstado = EstadoIdPorDescripcion(new[] { nuevoEstado });
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

        // Visualización de la postulación


        private int EstadoIdPorDescripcion(IEnumerable<string> descripciones)
        {
            var set = descripciones.Select(d => d.Trim()).ToList();
            var estado = db.EstadosTB.FirstOrDefault(e => set.Contains(e.Descripcion));
            if (estado == null)
                throw new Exception("No se encontró un estado válido en EstadosTB: " + string.Join(" / ", set));
            return estado.IdEstado;
        }

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

            public bool TieneRelacionEnVacante { get; set; }
            public bool EstadoAcademico { get; set; }
            public string TipoMensaje { get; set; }
        }


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
                    var especialidadEstudiante = dbContext.UsuarioEspecialidadTB
                      .Where(ue => ue.IdUsuario == idUsuario && ue.IdEstado == 1)
                      .Select(ue => ue.IdEspecialidad)
                      .FirstOrDefault();

                    if (especialidadEstudiante == 0)
                    {
                        return Json(new { success = false, message = "El estudiante no tiene una especialidad asignada" });
                    }

                    var modalidad = dbContext.ModalidadesTB.FirstOrDefault(m => m.IdModalidad == model.IdModalidad);
                    if (modalidad == null)
                    {
                        return Json(new { success = false, message = "La modalidad seleccionada no es válida" });
                    }


                    var estadoActivo = dbContext.EstadosTB.FirstOrDefault(e => e.IdEstado == 1);
                    if (estadoActivo == null)
                    {

                        var nuevoEstado = new EstadosTB { Descripcion = "Activo" };
                        dbContext.EstadosTB.Add(nuevoEstado);
                        dbContext.SaveChanges();
                        estadoActivo = nuevoEstado;
                    }

                    var provincia = dbContext.ProvinciasTB.FirstOrDefault(p => p.Nombre == model.Provincia);
                    if (provincia == null)
                    {
                        provincia = new ProvinciasTB { Nombre = model.Provincia };
                        dbContext.ProvinciasTB.Add(provincia);
                        dbContext.SaveChanges();
                    }


                    var canton = dbContext.CantonesTB.FirstOrDefault(c => c.Nombre == model.Canton && c.IdProvincia == provincia.IdProvincia);
                    if (canton == null)
                    {
                        canton = new CantonesTB { Nombre = model.Canton, IdProvincia = provincia.IdProvincia };
                        dbContext.CantonesTB.Add(canton);
                        dbContext.SaveChanges();
                    }

                    var distrito = dbContext.DistritosTB.FirstOrDefault(d => d.Nombre == model.Distrito && d.IdCanton == canton.IdCanton);
                    if (distrito == null)
                    {
                        distrito = new DistritosTB { Nombre = model.Distrito, IdCanton = canton.IdCanton };
                        dbContext.DistritosTB.Add(distrito);
                        dbContext.SaveChanges();
                    }


                    var direccion = new DireccionesTB
                    {
                        DireccionExacta = model.DireccionExacta.Trim(),
                        IdDistrito = distrito.IdDistrito,
                        IdEstado = estadoActivo.IdEstado
                    };

                    dbContext.DireccionesTB.Add(direccion);
                    dbContext.SaveChanges();


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

                    var estadoPendiente = dbContext.EstadosTB.FirstOrDefault(e => e.Descripcion == "Pendiente de Aprobación");
                    if (estadoPendiente == null)
                    {
                        estadoPendiente = new EstadosTB { Descripcion = "Pendiente de Aprobación" };
                        dbContext.EstadosTB.Add(estadoPendiente);
                        dbContext.SaveChanges();
                    }

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


                    var especialidadVacante = new EspecialidadesVacantesTB
                    {
                        IdEspecialidad = especialidadEstudiante,
                        IdVacante = vacante.IdVacante
                    };
                    dbContext.EspecialidadesVacantesTB.Add(especialidadVacante);
                    dbContext.SaveChanges();


                    var practica = new PracticaEstudianteTB
                    {
                        IdVacante = vacante.IdVacante,
                        IdUsuario = idUsuario,
                        FechaAplicacion = DateTime.Now,
                        IdEstado = estadoPendiente.IdEstado
                    };

                    dbContext.PracticaEstudianteTB.Add(practica);


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

                System.Diagnostics.Debug.WriteLine($"Error en RegistrarAutogestion: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }
        // ==============================
        // INICIAR TODAS LAS PRÁCTICAS
        // ==============================
        [HttpPost]
        public JsonResult IniciarPracticas()
        {
            try
            {
                if (Session["IdRol"] == null || Convert.ToInt32(Session["IdRol"]) != 2)
                    return Json(new { ok = false, message = "Solo el Coordinador puede iniciar prácticas." });

                if (Session["IdUsuario"] == null)
                    return Json(new { ok = false, message = "Sesión expirada." });

                int idUsuarioCoord = Convert.ToInt32(Session["IdUsuario"]);

                using (var db = new SIGEPEntities())
                {
                    db.Database.ExecuteSqlCommand(
                        "EXEC IniciarPracticasSP @IdUsuarioCoordinador",
                        new SqlParameter("@IdUsuarioCoordinador", idUsuarioCoord)
                    );
                }

                return Json(new { ok = true, message = "✅ Proceso iniciado: prácticas 'Asignadas' pasaron a 'En Curso' y las demás a 'Retirada'." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al iniciar prácticas: " + ex.Message });
            }
        }

        // ==============================
        // FINALIZAR TODAS LAS PRÁCTICAS
        // ==============================
        [HttpPost]
        public JsonResult FinalizarPracticas()
        {
            try
            {
                if (Session["IdRol"] == null || Convert.ToInt32(Session["IdRol"]) != 2)
                    return Json(new { ok = false, message = "Solo el Coordinador puede finalizar prácticas." });

                if (Session["IdUsuario"] == null)
                    return Json(new { ok = false, message = "Sesión expirada." });

                int idUsuarioCoord = Convert.ToInt32(Session["IdUsuario"]);

                using (var db = new SIGEPEntities())
                {
                    db.Database.ExecuteSqlCommand(
                        "EXEC FinalizarPracticasSP @IdUsuarioCoordinador",
                        new SqlParameter("@IdUsuarioCoordinador", idUsuarioCoord)
                    );
                }

                return Json(new
                {
                    ok = true,
                    message = "✅ Prácticas finalizadas: estados actualizados, vacantes archivadas y estudiantes egresados correctamente."
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Error al finalizar prácticas: " + ex.Message });
            }
        }
    }
}