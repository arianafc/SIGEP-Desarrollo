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
    [FiltroSesion]
    [ValidarUsuarioActivo]
    [FiltroUsuarioAdmin]
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
            ViewBag.Estados = ObtenerEstados();

            int idRol = 0;
            if (Session["IdRol"] != null)
                int.TryParse(Session["IdRol"].ToString(), out idRol);

            if (idRol == 3) // Profesor
            {
                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);


                var especialidadesProfesor = (from ue in db.UsuarioEspecialidadTB
                                              join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                                              where ue.IdUsuario == idUsuario
                                              select new SelectListItem
                                              {
                                                  Value = ue.IdEspecialidad.ToString(),
                                                  Text = esp.Nombre
                                              })
                                              .Distinct()
                                              .OrderBy(x => x.Text)
                                              .ToList();

                ViewBag.Especialidades = especialidadesProfesor;
            }
            else
            {
                ViewBag.Especialidades = ObtenerEspecialidades();
            }

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

            var query =
                from u in db.UsuariosTB
                join e in db.EstadosTB on u.IdEstado equals e.IdEstado into je
                from e in je.DefaultIfEmpty()
                where u.IdRol == 1 // <-- SOLO ESTUDIANTES
                select new EstudianteDTO
                {
                    IdUsuario = u.IdUsuario,
                    Cedula = u.Cedula,
                    NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,

                    Telefono = db.TelefonosTB.Where(t => t.IdUsuario == u.IdUsuario)
                              .OrderBy(t => t.IdTelefono)
                              .Select(t => t.Telefono)
                              .FirstOrDefault(),

                    IdEspecialidad = db.UsuarioEspecialidadTB
                        .Where(ue => ue.IdUsuario == u.IdUsuario && ue.IdEstado == 1)
                        .OrderByDescending(ue => ue.IdUsuarioEspecialidad)
                        .Select(ue => ue.IdEspecialidad)
                        .FirstOrDefault(),

                    EspecialidadNombre =
         (from ue in db.UsuarioEspecialidadTB
          join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
          where ue.IdUsuario == u.IdUsuario
                && ue.IdEstado == 1      
          orderby ue.IdUsuarioEspecialidad descending
          select esp.Nombre).FirstOrDefault(),

                    IdEstado = u.IdEstado,
                    EstadoAcademico = (bool)u.EstadoAcademico,
                    EstadoNombre = e != null ? e.Descripcion : "",

                    EstadoPractica =
         (from p in db.PracticaEstudianteTB
          join ep in db.EstadosTB on p.IdEstado equals ep.IdEstado
          where p.IdUsuario == u.IdUsuario
          orderby p.IdPractica descending
          select ep.Descripcion.Trim()).FirstOrDefault()
                };



            if (!string.IsNullOrWhiteSpace(estado))
            {
                var estadoNorm = estado.Trim().ToLowerInvariant();

                if (estadoNorm == "aprobada")
                {
                    query = query.Where(x => x.EstadoAcademico == true);
                }
                else if (estadoNorm == "rezagado")
                {
                    query = query.Where(x => x.EstadoAcademico == false);
                }
            }

            // Rol desde sesión: 1=Estudiante, 2=Coordinador, 3=Profesor, 4=Egresado
            int idRol = 0;
            if (Session["IdRol"] != null) int.TryParse(Session["IdRol"].ToString(), out idRol);

            if (idRol == 3) // Profesor
            {
                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);


                var especialidadesProfesor = db.UsuarioEspecialidadTB
                    .Where(ue => ue.IdUsuario == idUsuario && ue.IdEstado == 1)
                    .Select(ue => ue.IdEspecialidad)
                    .Distinct()
                    .ToList();


                if (especialidadesProfesor.Count > 1)
                {
                    if (idEspecialidad > 0)
                    {
                        query = query.Where(x => x.IdEspecialidad == idEspecialidad);
                    }
                    else
                    {
                        query = query.Where(x => especialidadesProfesor.Contains(x.IdEspecialidad));
                    }
                }
                else if (especialidadesProfesor.Count == 1)
                {
                    int idEsp = especialidadesProfesor.First();
                    query = query.Where(x => x.IdEspecialidad == idEsp);
                }
            }
            else
            {

                if (idEspecialidad > 0)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidad);
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
                EstadoAcademico = x.EstadoAcademico,

                EstadoNombre = x.EstadoAcademico ? "Aprobada" : "Rezagado",

                EstadoPractica = string.IsNullOrWhiteSpace(x.EstadoPractica)
        ? "Sin proceso activo"
        : x.EstadoPractica
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
                // LEFT JOIN Direcciones y Secciones
                var baseInfo = (from u in db.UsuariosTB
                                join d in db.DireccionesTB on u.IdDireccion equals d.IdDireccion into jd
                                from d in jd.DefaultIfEmpty()

                                join dist in db.DistritosTB on d.IdDistrito equals dist.IdDistrito into jdistr
                                from dist in jdistr.DefaultIfEmpty()

                                join cant in db.CantonesTB on dist.IdCanton equals cant.IdCanton into jc
                                from cant in jc.DefaultIfEmpty()


                                join prov in db.ProvinciasTB on cant.IdProvincia equals prov.IdProvincia into jp
                                from prov in jp.DefaultIfEmpty()


                                join s in db.SeccionesTB on u.IdSeccion equals s.IdSeccion into js
                                from s in js.DefaultIfEmpty()

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
                                    Provincia = prov != null ? prov.Nombre : "",
                                    Canton = cant != null ? cant.Nombre : "",
                                    Distrito = dist != null ? dist.Nombre : "",
                                    Seccion = s != null ? s.Seccion : ""
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


                int edad = 0;
                if (baseInfo.FechaNacimiento != default(DateTime))
                {
                    var nacimiento = baseInfo.FechaNacimiento;
                    var hoy = DateTime.Today;
                    edad = hoy.Year - nacimiento.Year;
                    if (nacimiento > hoy.AddYears(-edad)) edad--;
                }

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
                    Direccion = $"{baseInfo.Provincia}, {baseInfo.Canton}, {baseInfo.Distrito}, {baseInfo.DireccionExacta}".Trim(' ', ','),
                    EstadoPractica = estadoPractica,
                    Documentos = documentos,
                    Encargados = encargados,
                    Practicas = practicas,
                    Seccion = baseInfo.Seccion ?? ""
                };

                return PartialView("_DetalleEstudiante", estudiante);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error al cargar el perfil: " + ex.Message);
            }
        }

        // ==============================
        // DOCUMENTOS
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

        [HttpGet]
        public ActionResult DescargarDocumento(int id)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == id);

                    if (documento == null)
                    {
                        return HttpNotFound("Documento no encontrado");
                    }

                    // Convertir ruta relativa a física
                    string rutaFisica = Server.MapPath("~" + documento.RutaArchivo);

                    if (!System.IO.File.Exists(rutaFisica))
                    {
                        return HttpNotFound("Archivo no encontrado en el servidor");
                    }

                    var fileBytes = System.IO.File.ReadAllBytes(rutaFisica);
                    var extension = System.IO.Path.GetExtension(documento.Documento).ToLower();

                    string contentType = "application/octet-stream";
                    if (extension == ".pdf")
                        contentType = "application/pdf";
                    else if (extension == ".xlsx")
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    else if (extension == ".xls")
                        contentType = "application/vnd.ms-excel";

                    return File(fileBytes, contentType, documento.Documento);
                }
            }
            catch (Exception ex)
            {
                return Content("Error al descargar: " + ex.Message);
            }
        }


        [HttpGet]
        public ActionResult VisualizarDocumento(int id)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == id);

                    if (documento == null)
                    {
                        return HttpNotFound("Documento no encontrado");
                    }

                    // Convertir ruta relativa a física
                    string rutaFisica = Server.MapPath("~" + documento.RutaArchivo);

                    if (!System.IO.File.Exists(rutaFisica))
                    {
                        return HttpNotFound("Archivo no encontrado");
                    }

                    var extension = System.IO.Path.GetExtension(documento.Documento).ToLower();

                    // Solo permitir visualización de PDFs en el navegador
                    if (extension == ".pdf")
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(rutaFisica);
                        return File(fileBytes, "application/pdf");
                    }
                    else
                    {
                        // Para Excel, forzar descarga
                        return DescargarDocumento(id);
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult EliminarDocumento(int id)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == id);

                    if (documento == null)
                    {
                        return Json(new { success = false, message = "Documento no encontrado en la base de datos" });
                    }

                    // Convertir ruta relativa a física
                    string rutaFisica = Server.MapPath("~" + documento.RutaArchivo);

                    // Eliminar el registro de la base de datos
                    dbContext.DocumentosTB.Remove(documento);
                    dbContext.SaveChanges();

                    // Eliminar el archivo físico si existe
                    if (System.IO.File.Exists(rutaFisica))
                    {
                        try
                        {
                            System.IO.File.Delete(rutaFisica);
                        }
                        catch (Exception exFile)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al eliminar archivo físico: {exFile.Message}");
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Documento eliminado correctamente"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarDocumento: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = "Error al eliminar el documento: " + ex.Message
                });
            }
        }

        // ==============================
        // AUXILIARES
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
                    }).ToList();
        }

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

                        EstadoDescripcion = e.Descripcion,
                        Cedula = u.Cedula,
                        NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,
                        Empresa = emp.NombreEmpresa,
                        Estado = e.Descripcion,
                        IdPostulacion = p.IdPractica
                    }).ToList();
        }

        private List<SelectListItem> ObtenerEstados()
        {
            
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

        // ==============================
        // ACTUALIZAR ESTADO ACADÉMICO
        // ==============================

        [HttpPost]
        public JsonResult ActualizarEstado(int idUsuario, int nuevoEstadoId)
        {
            try
            {
                var usuario = db.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);
                if (usuario == null)
                    return Json(new { success = false, message = "Estudiante no encontrado" });

                var estado = db.EstadosTB.FirstOrDefault(e => e.IdEstado == nuevoEstadoId);
                if (estado == null)
                    return Json(new { success = false, message = "Estado no válido" });

                var desc = (estado.Descripcion ?? "").Trim().ToLowerInvariant();

                
                if (desc != "aprobada" && desc != "rezagado")
                    return Json(new { success = false, message = "Solo se permite cambiar a Rezagado o Aprobado." });

              
                usuario.IdEstado = nuevoEstadoId;
                usuario.EstadoAcademico = (desc == "aprobada");
                db.SaveChanges();

                
                if (desc == "rezagado")
                {
                    
                    var practica = db.PracticaEstudianteTB
                                     .Where(p => p.IdUsuario == idUsuario)
                                     .OrderByDescending(p => p.IdPractica)
                                     .FirstOrDefault();

                    if (practica != null)
                    {
                        
                        var estadoRetirada = db.EstadosTB
                            .FirstOrDefault(e => e.Descripcion.Trim().ToLower() == "retirada");

                        if (estadoRetirada != null)
                        {
                            
                            if (practica.IdEstado != estadoRetirada.IdEstado)
                            {
                                practica.IdEstado = estadoRetirada.IdEstado;
                                db.SaveChanges();
                            }
                        }
                    }
                }


                return Json(new
                {
                    success = true,
                    message = $"Estado académico actualizado a {(usuario.EstadoAcademico == true ? "Aprobado" : "Rezagado")} correctamente."
                });
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
                var practica = db.PracticaEstudianteTB
                                 .Where(p => p.IdUsuario == idUsuario)
                                 .OrderByDescending(p => p.IdPractica)
                                 .FirstOrDefault();

                if (practica == null)
                    return Json(new { success = false, message = "No se encontró ninguna práctica asociada al estudiante." });

                var estadosValidos = db.EstadosTB
                    .Where(e => new[] { "En proceso de Aplicacion", "Asignada", "Rechazada", "En curso", "Finalizada", "Aprobada", "Retirada", "Archivado" }
                    .Contains(e.Descripcion))
                    .Select(e => e.IdEstado)
                    .ToList();

                if (!estadosValidos.Contains(nuevoEstadoId))
                    return Json(new { success = false, message = "El estado seleccionado no es válido para prácticas." });

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
