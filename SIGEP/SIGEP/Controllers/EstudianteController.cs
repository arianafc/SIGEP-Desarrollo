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
            var estadosPracticaValidos = new List<string>
    {
        "en progreso", "asignada", "rechazada",
        "en curso", "finalizada", "aprobada", "retirada"
    };

            var estadosAcademicosValidos = new List<string> { "Aprobada", "Rezagado" };

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
                         where p.IdUsuario == u.IdUsuario &&
                            estadosPracticaValidos.Contains(ep.Descripcion.Trim().ToLower())
                         orderby p.IdPractica descending
                         select ep.Descripcion.Trim()).FirstOrDefault()
                };

            // 🔎 Filtro por estado académico
            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(x => x.EstadoNombre.ToLower().Trim() == estado.ToLower().Trim());
            }

            //filtro especialidad
            if (idEspecialidad > 0)
            {
                query = query.Where(x => x.IdEspecialidad == idEspecialidad);
            }

            // 🔒 Restricción para profesor
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
                EstadoNombre = string.IsNullOrEmpty(x.EstadoNombre) ? "Sin estado" : x.EstadoNombre,
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
            try
            {
                // LEFT JOIN a DireccionesTB y SeccionesTB para traer Seccion por nombre
                var baseInfo = (from u in db.UsuariosTB
                                join d in db.DireccionesTB on u.IdDireccion equals d.IdDireccion into jd
                                from d in jd.DefaultIfEmpty() // left join direcciones
                                join s in db.SeccionesTB on u.IdSeccion equals s.IdSeccion into js
                                from s in js.DefaultIfEmpty() // left join secciones
                                where u.IdUsuario == id
                                select new
                                {
                                    u.IdUsuario,
                                    u.Cedula,
                                    u.Nombre,
                                    u.Apellido1,
                                    u.Apellido2,
                                    u.FechaNacimiento,
                                    DireccionExacta = d != null ? d.DireccionExacta : "",
                                    Seccion = s != null ? s.Seccion : ""   // <--- AQUI traemos el nombre de la sección
                                }).FirstOrDefault();

                if (baseInfo == null)
                    return HttpNotFound("No se encontró el estudiante.");

                var correo = db.EmailsTB
                    .Where(e => e.IdUsuario == id)
                    .OrderByDescending(e => e.IdEmail)
                    .Select(e => e.Email)
                    .FirstOrDefault() ?? "";

                var telefono = db.TelefonosTB
                    .Where(t => t.IdUsuario == id)
                    .OrderByDescending(t => t.IdTelefono)
                    .Select(t => t.Telefono)
                    .FirstOrDefault() ?? "";

                var especialidad = (from ue in db.UsuarioEspecialidadTB
                                    join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                                    where ue.IdUsuario == id
                                    orderby ue.IdUsuarioEspecialidad descending
                                    select esp.Nombre).FirstOrDefault() ?? "Sin especialidad";

                var estadoPractica = (from p in db.PracticaEstudianteTB
                                      join es in db.EstadosTB on p.IdEstado equals es.IdEstado
                                      where p.IdUsuario == id
                                      orderby p.IdPractica descending
                                      select es.Descripcion).FirstOrDefault() ?? "No Asignada";

                // Calcular edad (si FechaNacimiento está seteada)
                int edad = 0;
                if (baseInfo.FechaNacimiento != default(DateTime))
                {
                    var nacimiento = baseInfo.FechaNacimiento;
                    var hoy = DateTime.Today;
                    edad = hoy.Year - nacimiento.Year;
                    if (nacimiento > hoy.AddYears(-edad)) edad--;
                }

                // Llamadas seguras a helpers
                var documentos = new List<DocumentoDTO>();
                var encargados = new List<EncargadoDTO>();
                var practicas = new List<PracticaEstudianteViewModel>();

                try { documentos = ObtenerDocumentosPorEstudiante(id); } catch { }
                try { encargados = ObtenerEncargadosPorEstudiante(id); } catch { }
                try { practicas = ObtenerPracticasPorEstudiante(id); } catch { }

                var estudiante = new EstudianteDetalleDTO
                {
                    IdUsuario = baseInfo.IdUsuario,
                    Cedula = baseInfo.Cedula,
                    Nombre = baseInfo.Nombre,
                    Apellido1 = baseInfo.Apellido1,
                    Apellido2 = baseInfo.Apellido2,
                    Edad = edad,
                    Correo = correo,
                    Telefono = telefono,
                    Especialidad = especialidad,
                    Direccion = baseInfo.DireccionExacta,
                    EstadoPractica = estadoPractica,
                    Documentos = documentos,
                    Encargados = encargados,
                    Practicas = practicas,
                    Seccion = baseInfo.Seccion ?? ""   // <--- Asignación al DTO
                };

                return PartialView("_DetalleEstudiante", estudiante);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error al cargar el perfil: " + ex.Message);
            }
        }




        // ==============================
        // CRUD DOCUMENTOS (SP)
        // ==============================
        // ==============================
        // ==============================
        // VER DOCUMENTO
        // ==============================
        public ActionResult VisualizarDocumento(int id)
        {
            var doc = db.Database.SqlQuery<DocumentoDTO>(
                "EXEC sp_ObtenerDocumento @IdDocumento",
                new SqlParameter("@IdDocumento", id)
            ).FirstOrDefault();

            if (doc == null)
                return HttpNotFound("Documento no encontrado.");

            if (!System.IO.File.Exists(doc.RutaArchivo))
                return HttpNotFound("El archivo físico no existe en el servidor.");

            string contentType = MimeMapping.GetMimeMapping(doc.RutaArchivo);
            return File(doc.RutaArchivo, contentType);
        }


        // ==============================
        // SUBIR DOCUMENTO
        // ==============================
        [HttpPost]
        public ActionResult SubirDocumento(int idUsuario, HttpPostedFileBase archivo)
        {
            if (archivo != null && archivo.ContentLength > 0)
            {
                string carpetaDestino = Server.MapPath("~/Uploads/Documentos/");
                if (!Directory.Exists(carpetaDestino))
                    Directory.CreateDirectory(carpetaDestino);

                string nombreArchivo = Path.GetFileName(archivo.FileName);
                string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);
                archivo.SaveAs(rutaCompleta);

                string tipo = Path.GetExtension(nombreArchivo).TrimStart('.');

                db.Database.ExecuteSqlCommand(
                    "EXEC sp_InsertarDocumento @IdUsuario, @Documento, @Tipo, @RutaArchivo",
                    new SqlParameter("@IdUsuario", idUsuario),
                    new SqlParameter("@Documento", nombreArchivo),
                    new SqlParameter("@Tipo", tipo),
                    new SqlParameter("@RutaArchivo", rutaCompleta)
                );
            }

            return RedirectToAction("Detalle", new { id = idUsuario });
        }

        // ==============================
        // DESCARGAR DOCUMENTO
        // ==============================
        public ActionResult DescargarDocumento(int id)
        {
            var doc = db.Database.SqlQuery<DocumentoDTO>(
                "EXEC sp_ObtenerDocumento @IdDocumento",
                new SqlParameter("@IdDocumento", id)
            ).FirstOrDefault();

            if (doc == null)
                return HttpNotFound("Documento no encontrado.");

            if (!System.IO.File.Exists(doc.RutaArchivo))
                return HttpNotFound("El archivo físico no existe en el servidor.");

            string contentType = MimeMapping.GetMimeMapping(doc.RutaArchivo);
            return File(doc.RutaArchivo, contentType, doc.Documento);
        }


        // ==============================
        // ELIMINAR DOCUMENTO
        // ==============================
        [HttpPost]
        public JsonResult EliminarDocumento(int id)
        {
            try
            {
                // 1️⃣ Obtener la ruta del documento
                var doc = db.Database.SqlQuery<DocumentoDTO>(
                    "EXEC sp_ObtenerDocumento @IdDocumento",
                    new SqlParameter("@IdDocumento", id)
                ).FirstOrDefault();

                if (doc == null)
                    return Json(new { success = false, message = "Documento no encontrado en la base de datos." });

                //// 2️⃣ Eliminar el archivo físico si existe
                //if (!string.IsNullOrEmpty(doc.RutaArchivo) && System.IO.File.Exists(doc.RutaArchivo))
                //{
                //    System.IO.File.Delete(doc.RutaArchivo);
                //}

                // 3️⃣ Eliminar el registro de la base de datos
                db.Database.ExecuteSqlCommand(
                    "EXEC sp_EliminarDocumento @IdDocumento",
                    new SqlParameter("@IdDocumento", id)
                );

                // 4️⃣ Devolver éxito (para el Swal)
                return Json(new { success = true, message = "Documento eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar el documento: " + ex.Message });
            }
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
            // Solo traer los estados académicos válidos
            return db.EstadosTB
                     .Where(est => est.Descripcion == "Aprobada" || est.Descripcion == "Rezagado")
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


        // ==============================
        // ACTUALIZAR ESTADO DE PRÁCTICA
        // ==============================
        [HttpPost]
        public JsonResult ActualizarEstadoPractica(int idUsuario, int nuevoEstadoId)
        {
            try
            {
                // ✅ Buscar la última práctica del estudiante
                var practica = db.PracticaEstudianteTB
                                 .Where(p => p.IdUsuario == idUsuario)
                                 .OrderByDescending(p => p.IdPractica)
                                 .FirstOrDefault();

                if (practica == null)
                    return Json(new { success = false, message = "No se encontró ninguna práctica asociada al estudiante." });

                // ✅ Validar que el estado pertenece a los válidos de práctica
                var estadosValidos = db.EstadosTB
                    .Where(e => new[] { "En progreso", "Asignada", "Rechazada", "En curso", "Finalizada", "Aprobada", "Retirada" }
                    .Contains(e.Descripcion))
                    .Select(e => e.IdEstado)
                    .ToList();

                if (!estadosValidos.Contains(nuevoEstadoId))
                    return Json(new { success = false, message = "El estado seleccionado no es válido para prácticas." });

                // ✅ Actualizar la práctica
                practica.IdEstado = nuevoEstadoId;
                db.SaveChanges();

                return Json(new { success = true, message = "Estado de práctica actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar el estado de la práctica: " + ex.Message });
            }
        }



    }
}
