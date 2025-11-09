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

            
                string carpetaDestino = @"C:\SIGEP\Comunicados";
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
                                RutaArchivo = rutaCompleta,
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

                  
                    string carpetaDestino = @"C:\SIGEP\Comunicados";
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

                   
                    if (System.IO.File.Exists(doc.RutaArchivo))
                        System.IO.File.Delete(doc.RutaArchivo);

                    dbContext.DocumentosTB.Remove(doc);
                    dbContext.SaveChanges();

                    return Json(new { success = true, message = "Documento eliminado correctamente" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }


        [HttpPost]
        public ActionResult EnviarCorreo(string Poblacion, string Asunto, string Mensaje, HttpPostedFileBase Archivo)
        {
            if (Session["IdRol"] == null) return new HttpStatusCodeResult(401);
            
            if (string.IsNullOrWhiteSpace(Poblacion) || string.IsNullOrWhiteSpace(Asunto) || string.IsNullOrWhiteSpace(Mensaje))
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                // Si hay adjunto, lo cargamos en memoria para reusarlo por destinatario
                byte[] adjBytes = null;
                string adjFilename = null;
                string adjMediaType = null;

                if (Archivo != null && Archivo.ContentLength > 0)
                {
                    using (var br = new BinaryReader(Archivo.InputStream))
                        adjBytes = br.ReadBytes(Archivo.ContentLength);
                    adjFilename = Path.GetFileName(Archivo.FileName);
                    adjMediaType = Archivo.ContentType;
                }

                var destinatarios = ObtenerDestinatarios(Poblacion);
                if (!destinatarios.Any())
                    return Json(new { ok = false, msg = "No hay destinatarios activos para la población seleccionada." });

                //var html = _utils.PlantillaComunicado(Asunto, Mensaje, null);

                foreach (var correo in destinatarios)
                {
                    System.Net.Mail.Attachment adj = null;
                    if (adjBytes != null)
                        adj = new System.Net.Mail.Attachment(new MemoryStream(adjBytes), adjFilename, adjMediaType);

                    // _utils.EnviarCorreo(correo, html, Asunto, adj);
                }

                return Json(new { ok = true, msg = "Correos enviados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = utilitarios.ObtenerMensajeSQL(ex) ?? "Error al enviar correos." });
            }
        }

        private System.Collections.Generic.List<string> ObtenerDestinatarios(string poblacion)
        {
            var p = (poblacion ?? "").Trim().ToLowerInvariant();

            // Usuarios ACTIVOS (IdEstado = 1)
            var q = db.UsuariosTB
                      .AsNoTracking()
                      .Where(u => u.IdEstado == 1);

            // Filtrado por población usando IdRol (1 Estudiante, 3 Profesor, 4 Egresado)
            switch (p)
            {
                case "profesores":
                    q = q.Where(u => u.IdRol == 3);
                    break;

                case "estudiantes":
                    // Si tus tablas de especialidad NO tienen IdEstado, quita esas condiciones.
                    q = q.Where(u => u.IdRol == 1
                                  && u.UsuarioEspecialidadTB.Any(ue =>
                                         /* si tu relación tiene estado, déjalo así: */ ue.IdEstado == 1
                                         /* y si EspecialidadesTB tiene estado: */     && ue.EspecialidadesTB.IdEstado == 1
                                      ));
                    break;

                case "egresados":
                    q = q.Where(u => u.IdRol == 4);
                    break;

                case "general":
                default:
                    // todos los usuarios activos
                    break;
            }

            // Emails válidos (SIN IdEstado en EmailsTB)
            var correos = q.SelectMany(u => u.EmailsTB)
                           .Where(e => e.Email != null && e.Email != "")
                           .Select(e => e.Email.Trim())
                           .Where(e => e.Length > 0)
                           .Distinct()
                           .ToList();

            return correos;
        }


    }
}
