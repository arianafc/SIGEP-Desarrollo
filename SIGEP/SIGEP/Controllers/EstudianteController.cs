using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    //[FiltroSesion]
    //[FiltroUsuarioAdmin]
    public class EstudianteController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();
        private readonly string _connectionString = "Server=localhost;Database=SIGEP;Integrated Security=True;TrustServerCertificate=True;";

        // ==============================
        // VISTA PRINCIPAL ESTUDIANTES
        // ==============================
        [HttpGet]
        public ActionResult Estudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Estados = ObtenerEstados();
            return View();
        }

        // ==============================
        // LISTADO ESTUDIANTES (JSON para DataTable)
        // ==============================
        [HttpGet]
        public JsonResult GetEstudiantes(string estado = "", int idEspecialidad = 0)
        {
            var query =
                from u in db.UsuariosTB
                join e in db.EstadosTB on u.IdEstado equals e.IdEstado into je
                from e in je.DefaultIfEmpty()
                select new EstudianteDTO
                {
                    IdUsuario = u.IdUsuario,
                    Cedula = u.Cedula,
                    NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,

                    Telefono = db.TelefonosTB
                                 .Where(t => t.IdUsuario == u.IdUsuario)
                                 .OrderBy(t => t.IdTelefono)
                                 .Select(t => t.Telefono)
                                 .FirstOrDefault(),

                    IdEspecialidad = db.UsuarioEspecialidadTB
                                       .Where(ue => ue.IdUsuario == u.IdUsuario)
                                       .OrderByDescending(ue => ue.IdUsuarioEspecialidad)
                                       .Select(ue => ue.IdEspecialidad)
                                       .FirstOrDefault(),

                    EspecialidadNombre =
                        (from ue in db.UsuarioEspecialidadTB
                         join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                         where ue.IdUsuario == u.IdUsuario
                         orderby ue.IdUsuarioEspecialidad descending
                         select esp.Nombre).FirstOrDefault(),

                    IdEstado = u.IdEstado,
                    EstadoNombre = e != null ? e.Descripcion : "",

                    EstadoPractica =
                        (from p in db.PracticaEstudianteTB
                         join ep in db.EstadosTB on p.IdEstado equals ep.IdEstado
                         where p.IdUsuario == u.IdUsuario
                         orderby p.IdPractica descending
                         select ep.Descripcion).FirstOrDefault()
                };

            // 🔎 Filtros
            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "ConPractica")
                    query = query.Where(x => !string.IsNullOrEmpty(x.EstadoPractica));
                else if (estado == "SinPractica")
                    query = query.Where(x => string.IsNullOrEmpty(x.EstadoPractica));
                else
                    query = query.Where(x => x.EstadoNombre == estado);
            }

            if (idEspecialidad > 0)
                query = query.Where(x => x.IdEspecialidad == idEspecialidad);

            // 🔎 Restricción si es Profesor
            var rolUsuario = Session["Rol"] != null ? Session["Rol"].ToString() : "";
            if (rolUsuario == "Profesor")
            {
                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
                int? idEspecialidadProfesor = db.UsuarioEspecialidadTB
                                                .Where(ue => ue.IdUsuario == idUsuario)
                                                .Select(ue => ue.IdEspecialidad)
                                                .FirstOrDefault();

                if (idEspecialidadProfesor.HasValue)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidadProfesor.Value);
            }

            // 📌 Proyección final
            var list = query.OrderByDescending(x => x.IdUsuario).ToList();
            var outList = list.Select(x => new
            {
                x.IdUsuario,
                x.Cedula,
                x.NombreCompleto,
                x.Telefono,
                x.IdEspecialidad,
                x.EspecialidadNombre,
                x.IdEstado,
                x.EstadoNombre,
                EstadoPractica = string.IsNullOrEmpty(x.EstadoPractica) ? "No Asignada" : x.EstadoPractica
            }).ToList();

            return Json(new { data = outList }, JsonRequestBehavior.AllowGet);
        }

        // ==============================
        // DETALLE DE ESTUDIANTE
        // ==============================
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            var baseInfo = (from u in db.UsuariosTB
                            join d in db.DireccionesTB on u.IdDireccion equals d.IdDireccion into jd
                            from d in jd.DefaultIfEmpty()
                            where u.IdUsuario == id
                            select new
                            {
                                u.IdUsuario,
                                u.Cedula,
                                u.Nombre,
                                u.Apellido1,
                                u.Apellido2,
                                u.FechaNacimiento,
                                DireccionExacta = d != null ? d.DireccionExacta : null
                            }).FirstOrDefault();

            if (baseInfo == null)
                return HttpNotFound();

            var correo = db.EmailsTB.Where(e => e.IdUsuario == id).Select(e => e.Email).FirstOrDefault();
            var telefono = db.TelefonosTB.Where(t => t.IdUsuario == id).Select(t => t.Telefono).FirstOrDefault();
            var especialidad = (from ue in db.UsuarioEspecialidadTB
                                join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                                where ue.IdUsuario == id
                                orderby ue.IdUsuarioEspecialidad descending
                                select esp.Nombre).FirstOrDefault();

            var estadoPractica = (from p in db.PracticaEstudianteTB
                                  join es in db.EstadosTB on p.IdEstado equals es.IdEstado
                                  where p.IdUsuario == id
                                  orderby p.IdPractica descending
                                  select es.Descripcion).FirstOrDefault();

            var hoy = DateTime.Today;
            var nacimiento = baseInfo.FechaNacimiento;
            var edad = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-edad)) edad--;

            var estudiante = new EstudianteDetalleDTO
            {
                IdUsuario = baseInfo.IdUsuario,
                Cedula = baseInfo.Cedula,
                Nombre = baseInfo.Nombre,
                Apellido1 = baseInfo.Apellido1,
                Apellido2 = baseInfo.Apellido2,
                Edad = edad,                      // ahora edad tiene set
                Correo = correo ?? string.Empty,
                Telefono = telefono ?? string.Empty,
                Especialidad = especialidad ?? string.Empty,
                Direccion = baseInfo.DireccionExacta ?? string.Empty,
                EstadoPractica = estadoPractica ?? "No Asignada"
            };

            // ---- NUEVO: llenar datos usados por la vista parcial
            ViewBag.Documentos = ObtenerDocumentosPorEstudiante(id);
            ViewBag.Encargados = ObtenerEncargadosPorEstudiante(id);
            ViewBag.Practicas = ObtenerPracticasPorEstudiante(id);

            return PartialView("_DetalleEstudiante", estudiante);
        }

        // ==============================
        // CRUD DOCUMENTOS (SP)
        // ==============================
        // ==============================
        // SUBIR DOCUMENTO
        // ==============================
        [HttpPost]
        public ActionResult SubirDocumento(int idEstudiante, HttpPostedFileBase archivo)
        {
            if (archivo != null && archivo.ContentLength > 0)
            {
                string fileName = Path.GetFileName(archivo.FileName);
                string path = Path.Combine(@"C:\Proyectos\Uploads\Documentos\", fileName);

                archivo.SaveAs(path);

                db.Database.ExecuteSqlCommand(
                    "EXEC sp_InsertarDocumento @IdUsuario, @Documento, @Tipo, @RutaArchivo",
                    new SqlParameter("@IdUsuario", idEstudiante),
                    new SqlParameter("@Documento", fileName),
                    new SqlParameter("@Tipo", Path.GetExtension(fileName)),
                    new SqlParameter("@RutaArchivo", path)
                );
            }

            return RedirectToAction("Detalle", new { id = idEstudiante });
        }

        // ==============================
        // DESCARGAR DOCUMENTO
        // ==============================
        public FileResult DescargarDocumento(int id)
        {
            var doc = db.Database.SqlQuery<DocumentoDTO>(
                "EXEC sp_ObtenerDocumento @IdDocumento",
                new SqlParameter("@IdDocumento", id)
            ).FirstOrDefault();

            if (doc == null) throw new Exception("Documento no encontrado");

            byte[] fileBytes = System.IO.File.ReadAllBytes(doc.RutaArchivo);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, doc.Documento);
        }

        // ==============================
        // ELIMINAR DOCUMENTO
        // ==============================
        [HttpPost]
        public JsonResult EliminarDocumento(int id)
        {
            var doc = db.Database.SqlQuery<DocumentoDTO>(
                "EXEC sp_ObtenerDocumento @IdDocumento",
                new SqlParameter("@IdDocumento", id)
            ).FirstOrDefault();

            if (doc != null)
            {
                if (System.IO.File.Exists(doc.RutaArchivo))
                    System.IO.File.Delete(doc.RutaArchivo);

                db.Database.ExecuteSqlCommand(
                    "EXEC sp_EliminarDocumento @IdDocumento",
                    new SqlParameter("@IdDocumento", id)
                );
            }

            return Json(new { success = true });
        }

        // ==============================
        // AUXILIAR: obtener documentos usando SP
        // ==============================
        private List<DocumentoDTO> ObtenerDocumentosPorEstudiante(int idUsuario)
        {
            return db.Database.SqlQuery<DocumentoDTO>(
                "EXEC sp_ObtenerDocumentosPorUsuario @IdUsuario",
                new SqlParameter("@IdUsuario", idUsuario)
            ).ToList();
        }

        private List<EncargadoDTO> ObtenerEncargadosPorEstudiante(int idUsuario)
        {
            return (from ee in db.EstudianteEncargadoTB
                    join e in db.EncargadosTB on ee.IdEncargado equals e.IdEncargado
                    where ee.IdUsuario == idUsuario
                    select new EncargadoDTO
                    {
                        IdEncargado = e.IdEncargado,
                        Nombre = e.Nombre + " " + e.Apellido1 + " " + e.Apellido2,
                        Telefono = (db.TelefonosTB
                                       .Where(t => t.IdEncargado == e.IdEncargado)
                                       .OrderBy(t => t.IdTelefono)
                                       .Select(t => t.Telefono)
                                       .FirstOrDefault()) ?? string.Empty,
                        Ocupacion = e.Ocupacion
                        // ❌ Eliminado: Parentesco y Direccion
                    }).ToList();
        }


        // ==============================
        // PRACTICAS DEL ESTUDIANTE
        // ==============================
        private List<PracticaEstudianteViewModel> ObtenerPracticasPorEstudiante(int idUsuario)
        {
            return (from p in db.PracticaEstudianteTB
                    join v in db.VacantesPracticasTB on p.IdVacante equals v.IdVacante
                    join e in db.EstadosTB on p.IdEstado equals e.IdEstado
                    join u in db.UsuariosTB on p.IdUsuario equals u.IdUsuario
                    join emp in db.EmpresasTB on v.IdEmpresa equals emp.IdEmpresa
                    where p.IdUsuario == idUsuario
                    orderby p.IdPractica descending
                    select new PracticaEstudianteViewModel
                    {
                        IdPractica = p.IdPractica,
                        IdVacante = v.IdVacante,
                        IdUsuario = u.IdUsuario,
                        FechaAplicacion = p.FechaAplicacion,
                        IdEstado = p.IdEstado,

                        // ---- Datos extendidos
                        EstadoDescripcion = e.Descripcion,
                        Cedula = u.Cedula,
                        NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                        Empresa = emp.NombreEmpresa,
                        Estado = e.Descripcion,
                        IdPostulacion = p.IdPractica   // aquí mapeamos IdPractica como IdPostulacion
                    }).ToList();
        }


        private List<SelectListItem> ObtenerEstados()
        {
            return db.EstadosTB
                     .Where(est => est.Descripcion == "Rezagado" || est.Descripcion == "Aprobada")
                     .OrderBy(est => est.Descripcion)
                     .Select(est => new SelectListItem
                     {
                         Value = est.IdEstado.ToString(),
                         Text = est.Descripcion
                     })
                     .ToList();
        }

        private List<SelectListItem> ObtenerEspecialidades()
        {
            return db.EspecialidadesTB
                     .OrderBy(esp => esp.Nombre)
                     .Select(esp => new { esp.IdEspecialidad, esp.Nombre })
                     .AsEnumerable()
                     .Select(x => new SelectListItem
                     {
                         Value = x.IdEspecialidad.ToString(),
                         Text = x.Nombre
                     })
                     .ToList();
        }

        [HttpPost]
        public JsonResult ActualizarEstado(int idUsuario, int nuevoEstadoId)
        {
            try
            {
                var usuario = db.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);
                if (usuario == null)
                    return Json(new { success = false, message = "Estudiante no encontrado" });

                // Validar que el nuevo estado sea Rezagado o Aprobado
                var estadosValidos = db.EstadosTB
                    .Where(e => e.Descripcion == "Rezagado" || e.Descripcion == "Aprobada")
                    .Select(e => e.IdEstado)
                    .ToList();

                if (!estadosValidos.Contains(nuevoEstadoId))
                    return Json(new { success = false, message = "Solo se permite cambiar a Rezagado o Aprobado." });

                usuario.IdEstado = nuevoEstadoId;
                db.SaveChanges();

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar: " + ex.Message });
            }
        }


    }
}
