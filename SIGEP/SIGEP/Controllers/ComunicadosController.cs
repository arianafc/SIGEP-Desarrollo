using Sigep.Models;
using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sigep.UI.Controllers
{
    public class ComunicadosController : Controller
    {
        private readonly Utilitarios utilitarios = new Utilitarios();
        
        
        private readonly SIGEPEntities db = new SIGEPEntities();

        [FiltroSesion]
        [HttpGet]
        public ActionResult Comunicados()
        {
            var model = new ComunicadosVM();
            ViewBag.IdRol = Session["IdRol"];

            using (var db = new SIGEPEntities())
            {
                model.AllComunicados = db.ComunicadosTB.Where(c => c.IdEstado == 1).Select(c => new ComunicadoCardVM
                {
                    Id = c.IdComunicado,
                    Titulo = c.Nombre,
                    FechaPublicacion = c.Fecha,
                    FechaAplicacion = c.FechaLimite,
                    Descripcion = c.Informacion,
                    PublicadoPor = c.UsuariosTB.Nombre + " " + c.UsuariosTB.Apellido1,
                    DirigidoA = c.Poblacion
                }).OrderByDescending(c => c.FechaPublicacion).ToList();
                model.ListaComunicadosGeneral = model.AllComunicados.Where(c => c.DirigidoA.ToLower() == "general").ToList();
                model.ListaComunicadosEstudiantes = model.AllComunicados.Where(c => c.DirigidoA.ToLower() == "estudiantes").ToList();
                model.ListaComunicadosProfesores = model.AllComunicados.Where(c => c.DirigidoA.ToLower() == "profesores").ToList();
                model.ListaComunicadosEgresados = model.AllComunicados.Where(c => c.DirigidoA.ToLower() == "egresados").ToList();
            }




            return View(model);
        }

        [HttpPost]
        public JsonResult EliminarComunicado(int IdComunicado)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var comunicado = db.ComunicadosTB.Find(IdComunicado);
                    if (comunicado == null)
                        return Json(new { ok = false, msg = "Comunicado no encontrado." });
                    comunicado.IdEstado = 2;
                    db.SaveChanges();
                    return Json(new { ok = true, msg = "Comunicado desactivado correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = utilitarios.ObtenerMensajeSQL(ex) ?? "Error al eliminar el comunicado." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearComunicado(string Titulo, string Descripcion, DateTime? FechaAplicacion, string DirigidoA, List<HttpPostedFileBase> archivos)
        {
      
            if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Descripcion) || string.IsNullOrWhiteSpace(DirigidoA))
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                int idUsuarioCreador = 0;
                if (Session["idUsuario"] != null)
                    int.TryParse(Session["idUsuario"].ToString(), out idUsuarioCreador);

                var nuevo = new ComunicadosTB
                {
                    Nombre = Titulo,
                    Informacion = Descripcion,
                    Fecha = DateTime.Now,
                    Poblacion = DirigidoA,
                    FechaLimite = FechaAplicacion,
                    IdUsuario = idUsuarioCreador,
                    IdEstado = 1
                };

                db.ComunicadosTB.Add(nuevo);
                db.SaveChanges();


                string carpetaDestino = Server.MapPath("~/Documentos/Comunicados/");
                if (!Directory.Exists(carpetaDestino))
                    Directory.CreateDirectory(carpetaDestino);

            
                if (archivos != null && archivos.Count > 0)
                {
                    foreach (var archivo in archivos)
                    {
                        if (archivo != null && archivo.ContentLength > 0)
                        {
                            string ext = Path.GetExtension(archivo.FileName);
                            string nombreArchivo = $"Comunicado{nuevo.IdComunicado}{ext}";
                            string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

                            archivo.SaveAs(rutaCompleta);

                            var doc = new DocumentosTB
                            {
                                Documento = nombreArchivo,
                                Tipo = ext,
                                RutaArchivo = "/Documentos/Comunicados/" + nombreArchivo,
                                FechaSubida = DateTime.Now,
                                IdUsuario = idUsuarioCreador,
                                IdComunicado = nuevo.IdComunicado
                            };

                            db.DocumentosTB.Add(doc);
                        }
                    }

                    db.SaveChanges();
                }

                return Json(new { ok = true, msg = "Comunicado publicado y documentos guardados correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = utilitarios.ObtenerMensajeSQL(ex) ?? "Error al guardar el comunicado." });
            }
        }

        [HttpPost]
        public ActionResult EditarComunicado(
                int IdComunicado,
                string Titulo,
                string Descripcion,
                DateTime? FechaAplicacion,
                string DirigidoA,
                List<HttpPostedFileBase> archivos)
        {
            if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Descripcion) || string.IsNullOrWhiteSpace(DirigidoA))
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                using (var db = new SIGEPEntities())
                {
                    var comunicado = db.ComunicadosTB.Find(IdComunicado);
                    if (comunicado == null)
                        return Json(new { ok = false, msg = "Comunicado no encontrado." });

                    int idUsuarioCreador = 0;
                    if (Session["idUsuario"] != null)
                        int.TryParse(Session["idUsuario"].ToString(), out idUsuarioCreador);

                   
                    comunicado.Nombre = Titulo;
                    comunicado.Informacion = Descripcion;
                    comunicado.Poblacion = DirigidoA;
                    comunicado.FechaLimite = FechaAplicacion;
                    comunicado.Fecha = DateTime.Now;
                    db.SaveChanges();


                    string carpetaDestino = Server.MapPath("~/Documentos/Comunicados/");
                    if (!Directory.Exists(carpetaDestino))
                        Directory.CreateDirectory(carpetaDestino);

                  
                    if (archivos != null && archivos.Count > 0)
                    {
                        foreach (var archivo in archivos)
                        {
                            if (archivo != null && archivo.ContentLength > 0)
                            {
                                string ext = Path.GetExtension(archivo.FileName);
                                string nombreArchivoBase = $"Comunicado{comunicado.IdComunicado}{ext}";
                                string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivoBase);

                             
                                if (System.IO.File.Exists(rutaCompleta))
                                {
                                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    string nombreArchivoConSufijo = $"Comunicado{comunicado.IdComunicado}_{timestamp}{ext}";
                                    rutaCompleta = Path.Combine(carpetaDestino, nombreArchivoConSufijo);
                                    nombreArchivoBase = nombreArchivoConSufijo;
                                }

                                
                                archivo.SaveAs(rutaCompleta);

                             
                                var doc = new DocumentosTB
                                {
                                    Documento = nombreArchivoBase,
                                    Tipo = ext,
                                    RutaArchivo = rutaCompleta,
                                    FechaSubida = DateTime.Now,
                                    IdUsuario = idUsuarioCreador,
                                    IdComunicado = comunicado.IdComunicado
                                };

                                db.DocumentosTB.Add(doc);
                            }
                        }

                        db.SaveChanges();
                    }

                    return Json(new { ok = true, msg = "Comunicado actualizado correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = utilitarios.ObtenerMensajeSQL(ex) ?? "Error al actualizar el comunicado." });
            }
        }


        [HttpGet]
        public JsonResult ObtenerDocumentos(int IdComunicado)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var documentos = db.DocumentosTB
                        .Where(d => d.IdComunicado == IdComunicado).AsEnumerable()
                        .Select(d => new
                        {
                            IdDocumento = d.IdDocumento,
                            Nombre = d.Documento,
                            RutaArchivo = d.RutaArchivo,
                            FechaSubida = d.FechaSubida.ToString("dd/MM/yyyy HH:mm")
                        })
                        .ToList();

                    return Json(new
                    {
                        success = true,
                        documentos = documentos
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error al obtener los documentos: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult EliminarDocumento(int idDocumento)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var doc = dbContext.DocumentosTB.Find(idDocumento);
                    if (doc == null)
                        return Json(new { success = false, message = "Documento no encontrado" });

                    string rutaFisica = Server.MapPath("~" + doc.RutaArchivo);

                    // Eliminar el registro de la base de datos
                    dbContext.DocumentosTB.Remove(doc);
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
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public ActionResult DescargarDocumento(int idDocumento)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == idDocumento);

                    if (documento == null)
                        return HttpNotFound("Documento no encontrado");

                    string rutaFisica = documento.RutaArchivo;

                    if (rutaFisica.StartsWith("~"))
                        rutaFisica = Server.MapPath(rutaFisica);

                    if (!System.IO.File.Exists(rutaFisica))
                        return HttpNotFound("Archivo no encontrado en el servidor");

                    var fileBytes = System.IO.File.ReadAllBytes(rutaFisica);
                    var extension = Path.GetExtension(documento.Documento).ToLower();

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



        [HttpPost]
        public ActionResult EnviarCorreo(string Poblacion, string Asunto, string Mensaje, List<HttpPostedFileBase> Archivos)
        {
          
            if (string.IsNullOrWhiteSpace(Poblacion) || string.IsNullOrWhiteSpace(Asunto) || string.IsNullOrWhiteSpace(Mensaje))
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                List<string> destinatarios;

                using (var db = new SIGEPEntities())
                {
                    destinatarios = db.EmailsTB
                        .Where(e => e.UsuariosTB.IdEstado == 1)
                        .Where(e =>
                            (Poblacion == "General" && e.UsuariosTB.RolesTB.Descripcion.ToLower() != "coordinador") ||
                            (Poblacion == "Profesores" && e.UsuariosTB.RolesTB.Descripcion.ToLower() == "profesor") ||
                            (Poblacion == "Estudiantes" && e.UsuariosTB.RolesTB.Descripcion.ToLower() == "estudiante") ||
                            (Poblacion == "Egresados" && e.UsuariosTB.RolesTB.Descripcion.ToLower() == "egresado")
                        )
                        .Select(e => e.Email)
                        .ToList();
                }

                if (!destinatarios.Any())
                    return Json(new { ok = false, msg = "No hay destinatarios activos para la población seleccionada." });

             
                var rutasAdjuntos = new List<string>();
                if (Archivos != null && Archivos.Any())
                {
                    foreach (var archivo in Archivos)
                    {
                        if (archivo != null && archivo.ContentLength > 0)
                        {
                            var nombreArchivo = Path.GetFileName(archivo.FileName);
                            var ruta = Path.Combine(Server.MapPath("~/Temp"), nombreArchivo);
                            archivo.SaveAs(ruta);
                            rutasAdjuntos.Add(ruta);
                        }
                    }
                }

                string cuerpoHtml = utilitarios.GenerarPlantillaCorreo("Comunicado SIGEP", Mensaje);

                int enviados = 0;
                foreach (var correo in destinatarios)
                {
                    try
                    {
                        bool ok;
                        if (rutasAdjuntos.Any())
                            ok = utilitarios.EnviarCorreoConAdjuntos(correo, cuerpoHtml, Asunto, rutasAdjuntos);
                        else
                            ok = utilitarios.EnviarCorreo(correo, cuerpoHtml, Asunto);

                        if (ok) enviados++;
                    }
                    catch { }
                }

                foreach (var ruta in rutasAdjuntos)
                {
                    if (System.IO.File.Exists(ruta))
                        System.IO.File.Delete(ruta);
                }

                return Json(new
                {
                    ok = true,
                    msg = $"Correos enviados exitosamente ({enviados} de {destinatarios.Count})."
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = utilitarios.ObtenerMensajeSQL(ex) ?? "Error al enviar correos." });
            }
        }

    }
}
