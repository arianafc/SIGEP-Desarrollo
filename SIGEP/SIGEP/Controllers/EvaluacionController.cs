using SIGEP.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class EvaluacionController : Controller
    {
        public ActionResult ListarEstudianteConPractica()
        {
            if (Session["IdUsuario"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerEstudiantes()
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
                }

                int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var estudiantes = dbContext.ObtenerEstudiantesParaEvaluacionSP(idProfesor)
                        .Select(e => new
                        {
                            IdUsuario = e.IdUsuario,
                            Cedula = e.Cedula,
                            NombreCompleto = e.NombreCompleto,
                            Especialidad = e.Especialidad,
                            Telefono = e.Telefono,
                            PracticaAsignada = e.PracticaAsignada,
                            EstadoAcademico = e.EstadoAcademico,
                            NotaFinal = e.NotaFinal
                        }).ToList();

                    return Json(estudiantes, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerPerfilEstudiante(int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var perfil = dbContext.ObtenerPerfilEstudianteSP(idUsuario).FirstOrDefault();

                    if (perfil == null)
                    {
                        return Json(new { success = false, message = "No se encontró el estudiante" }, JsonRequestBehavior.AllowGet);
                    }

                    var comentarios = dbContext.ObtenerComentariosEstudianteSP(idUsuario)
                        .Select(c => new
                        {
                            Autor = c.Autor,
                            Fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                            Comentario = c.Comentario
                        }).ToList();

                    var resultado = new
                    {
                        success = true,
                        perfil = new
                        {
                            NombreCompleto = perfil.NombreCompleto,
                            Correo = perfil.Correo,
                            Telefono = perfil.Telefono,
                            Direccion = perfil.Direccion,
                            Sexo = perfil.Sexo,
                            Especialidad = perfil.Especialidad,
                            Edad = perfil.Edad,
                            Seccion = perfil.Seccion,
                            NombreEmpresa = perfil.NombreEmpresa,
                            TelefonoEmpresa = perfil.TelefonoEmpresa,
                            IdVacante = perfil.IdVacante,    
                            EstadoPractica = perfil.EstadoPractica,
                            IdUsuario = perfil.IdUsuario,      
                            Comentarios = comentarios
                        }
                    };

                    return Json(resultado, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerComentarios(int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var comentarios = dbContext.ObtenerComentariosEstudianteSP(idUsuario)
                        .Select(c => new
                        {
                            Autor = c.Autor,
                            Fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                            Comentario = c.Comentario
                        }).ToList();

                    return Json(comentarios, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerNotas(int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var notas = dbContext.ObtenerNotasEstudianteSP(idUsuario).FirstOrDefault();

                    if (notas == null)
                    {
                        return Json(new { Nota1 = (decimal?)null, Nota2 = (decimal?)null, NotaFinal = (decimal?)null }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new
                    {
                        Nota1 = notas.Nota1,
                        Nota2 = notas.Nota2,
                        NotaFinal = notas.NotaFinal
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { Nota1 = (decimal?)null, Nota2 = (decimal?)null, NotaFinal = (decimal?)null }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult GuardarNota(int idUsuario, string nota1Str, string nota2Str)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

                // Convertir strings a decimal nullable
                decimal? nota1 = null;
                decimal? nota2 = null;

                if (!string.IsNullOrWhiteSpace(nota1Str))
                {
                    if (decimal.TryParse(nota1Str, out decimal n1))
                    {
                        nota1 = n1;
                    }
                }

                if (!string.IsNullOrWhiteSpace(nota2Str))
                {
                    if (decimal.TryParse(nota2Str, out decimal n2))
                    {
                        nota2 = n2;
                    }
                }

                // Validaciones de rango solo para notas que no son null
                if (nota1 != null && (nota1 < 0 || nota1 > 100))
                {
                    return Json(new { success = false, message = "La Nota 1 debe estar entre 0 y 100" });
                }

                if (nota2 != null && (nota2 < 0 || nota2 > 100))
                {
                    return Json(new { success = false, message = "La Nota 2 debe estar entre 0 y 100" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    var notaExistente = dbContext.NotasEstudiantesTB.FirstOrDefault(n => n.IdUsuario == idUsuario);

                    decimal? notaFinalCalculada = null;

                    if (notaExistente != null)
                    {
                        notaExistente.Nota1 = nota1;
                        notaExistente.Nota2 = nota2;

                        if (notaExistente.Nota1 != null && notaExistente.Nota2 != null)
                        {
                            notaExistente.NotaFinal = (notaExistente.Nota1 + notaExistente.Nota2) / 2;
                            notaFinalCalculada = notaExistente.NotaFinal;
                        }
                        else
                        {
                            notaExistente.NotaFinal = null;
                        }

                        notaExistente.FechaActualizacion = DateTime.Now;
                        notaExistente.IdProfesor = idProfesor;
                    }
                    else
                    {
                        decimal? notaFinal = null;
                        if (nota1 != null && nota2 != null)
                        {
                            notaFinal = (nota1 + nota2) / 2;
                            notaFinalCalculada = notaFinal;
                        }

                        var nuevaNota = new NotasEstudiantesTB
                        {
                            IdUsuario = idUsuario,
                            Nota1 = nota1,
                            Nota2 = nota2,
                            NotaFinal = notaFinal,
                            FechaRegistro = DateTime.Now,
                            IdProfesor = idProfesor
                        };
                        dbContext.NotasEstudiantesTB.Add(nuevaNota);
                    }

                    dbContext.SaveChanges();

                    // Cambio automático de estado
                    string mensajeEstado = "";
                    string estadoActual = null;

                    if (notaFinalCalculada.HasValue)
                    {
                        int anioActual = DateTime.Now.Year;
                        var estadosPermitidos = new[] { "En Curso", "Rezagado", "Aprobada", "Finalizada" };

                        var practicaActual = (from p in dbContext.PracticaEstudianteTB
                                              join e in dbContext.EstadosTB on p.IdEstado equals e.IdEstado
                                              where p.IdUsuario == idUsuario
                                                 && estadosPermitidos.Contains(e.Descripcion)
                                                 && p.FechaAplicacion.Year == anioActual
                                              orderby p.FechaAplicacion descending
                                              select new { Practica = p, Estado = e })
                                             .FirstOrDefault();

                        if (practicaActual != null)
                        {
                            string nuevoEstado;
                            int nuevoIdEstado;

                            if (notaFinalCalculada >= 70)
                            {
                                nuevoEstado = "Aprobada";
                                var estado = dbContext.EstadosTB.FirstOrDefault(e => e.Descripcion == nuevoEstado);
                                if (estado == null)
                                {
                                    return Json(new { success = true, message = $"Nota guardada pero no se encontró el estado '{nuevoEstado}'" });
                                }
                                nuevoIdEstado = estado.IdEstado;
                            }
                            else
                            {
                                nuevoEstado = "Rezagado";
                                var estado = dbContext.EstadosTB.FirstOrDefault(e => e.Descripcion == nuevoEstado);
                                if (estado == null)
                                {
                                    return Json(new { success = true, message = $"Nota guardada pero no se encontró el estado '{nuevoEstado}'" });
                                }
                                nuevoIdEstado = estado.IdEstado;
                            }

                            if (practicaActual.Practica.IdEstado != nuevoIdEstado)
                            {
                                string estadoAnterior = practicaActual.Estado.Descripcion;
                                practicaActual.Practica.IdEstado = nuevoIdEstado;

                                var comentarioAuto = new ComentariosPracticaTB
                                {
                                    Comentario = $"Estado actualizado de '{estadoAnterior}' a '{nuevoEstado}' por modificación de calificación final a {notaFinalCalculada.Value:F2}",
                                    Fecha = DateTime.Now,
                                    IdUsuario = idProfesor,
                                    IdPractica = practicaActual.Practica.IdPractica,
                                    Tipo = "Sistema"
                                };
                                dbContext.ComentariosPracticaTB.Add(comentarioAuto);
                                dbContext.SaveChanges();

                                mensajeEstado = $" Estado actualizado de '{estadoAnterior}' a '{nuevoEstado}'.";
                                estadoActual = nuevoEstado;
                            }
                            else
                            {
                                estadoActual = practicaActual.Estado.Descripcion;
                            }
                        }
                    }

                    // Mensajes personalizados
                    string mensaje = "Nota actualizada correctamente";
                    if (nota1 != null && nota2 != null)
                    {
                        mensaje = "Notas registradas correctamente. Nota final calculada." + mensajeEstado;
                    }
                    else if (nota1 != null && nota2 == null)
                    {
                        mensaje = "Nota 1 registrada. Ingrese Nota 2 para calcular la nota final.";
                    }
                    else if (nota1 == null && nota2 != null)
                    {
                        mensaje = "Nota 2 registrada. Ingrese Nota 1 para calcular la nota final.";
                    }
                    else if (nota1 == null && nota2 == null)
                    {
                        mensaje = "Notas eliminadas correctamente.";
                    }

                    return Json(new
                    {
                        success = true,
                        message = mensaje,
                        estadoPractica = estadoActual,
                        notaFinal = notaFinalCalculada
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar la nota: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GuardarComentario(int idUsuario, string comentario)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                int idProfesor = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    // Obtener el IdPractica del estudiante
                    var practica = dbContext.PracticaEstudianteTB.FirstOrDefault(p => p.IdUsuario == idUsuario);

                    if (practica == null)
                    {
                        return Json(new { success = false, message = "El estudiante no tiene práctica asignada" });
                    }

                    // Insertar comentario
                    var nuevoComentario = new ComentariosPracticaTB
                    {
                        Comentario = comentario,
                        Fecha = DateTime.Now,
                        IdUsuario = idProfesor,
                        IdPractica = practica.IdPractica,
                        Tipo = "Evaluación Tutor"
                    };

                    dbContext.ComentariosPracticaTB.Add(nuevoComentario);
                    dbContext.SaveChanges();

                    // Obtener nombre del profesor
                    var profesor = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idProfesor);
                    string nombreProfesor = profesor != null
                        ? $"{profesor.Nombre} {profesor.Apellido1}"
                        : "Profesor";

                    return Json(new
                    {
                        success = true,
                        message = "Comentario agregado exitosamente",
                        autor = nombreProfesor,
                        fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SubirDocumento(HttpPostedFileBase archivo, int idUsuario)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                if (archivo == null || archivo.ContentLength == 0)
                {
                    return Json(new { success = false, message = "No se seleccionó ningún archivo" });
                }

                // Validar extensión
                var extensionesPermitidas = new[] { ".xls", ".xlsx", ".pdf" };
                var extension = System.IO.Path.GetExtension(archivo.FileName).ToLower();

                if (!extensionesPermitidas.Contains(extension))
                {
                    return Json(new { success = false, message = "Solo se permiten archivos .xls, .xlsx o .pdf" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    // Obtener cédula del estudiante
                    var estudiante = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);
                    if (estudiante == null)
                    {
                        return Json(new { success = false, message = "Estudiante no encontrado" });
                    }

                    string cedulaEstudiante = estudiante.Cedula;

                    // Crear directorio en C:\sigep si no existe
                    string directorioBase = @"C:\sigep\Evaluaciones";
                    if (!System.IO.Directory.Exists(directorioBase))
                    {
                        System.IO.Directory.CreateDirectory(directorioBase);
                    }

                    // Generar nombre del archivo con cédula (sin fecha/hora)
                    string nombreOriginal = System.IO.Path.GetFileNameWithoutExtension(archivo.FileName);
                    string nombreArchivo = $"{cedulaEstudiante}_{nombreOriginal}{extension}";

                    // Ruta completa del archivo
                    string rutaCompleta = System.IO.Path.Combine(directorioBase, nombreArchivo);

                    // Si el archivo ya existe, se sobrescribe
                    archivo.SaveAs(rutaCompleta);

                    // Guardar registro en BD con la ruta del archivo
                    var documento = new DocumentosTB
                    {
                        Documento = archivo.FileName, // Nombre original para mostrar
                        Tipo = "Evaluación",
                        RutaArchivo = rutaCompleta,
                        FechaSubida = DateTime.Now,
                        IdUsuario = idUsuario
                    };

                    dbContext.DocumentosTB.Add(documento);
                    dbContext.SaveChanges();

                    return Json(new { success = true, message = "Documento subido correctamente" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerDocumentosEvaluacion(int idUsuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documentos = dbContext.ObtenerDocumentosEvaluacionSP(idUsuario).ToList();

                    var resultado = documentos.Select(d => new
                    {
                        IdDocumento = d.IdDocumento,
                        Nombre = d.Documento,
                        RutaArchivo = d.RutaArchivo,
                        FechaSubida = d.FechaSubida.ToString("dd/MM/yyyy HH:mm"),
                        Extension = d.Extension ?? System.IO.Path.GetExtension(d.Documento)
                    }).ToList();

                    return Json(new { success = true, documentos = resultado }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
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
                    {
                        return HttpNotFound("Documento no encontrado");
                    }

                    // Ya tenemos la ruta física completa, no usar Server.MapPath
                    var filePath = documento.RutaArchivo;

                    if (!System.IO.File.Exists(filePath))
                    {
                        return HttpNotFound("Archivo no encontrado en el servidor");
                    }

                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
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
        public ActionResult VisualizarDocumento(int idDocumento)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == idDocumento);

                    if (documento == null)
                    {
                        return HttpNotFound("Documento no encontrado");
                    }

                    // Ya tenemos la ruta física completa
                    var filePath = documento.RutaArchivo;

                    if (!System.IO.File.Exists(filePath))
                    {
                        return HttpNotFound("Archivo no encontrado");
                    }

                    var extension = System.IO.Path.GetExtension(documento.Documento).ToLower();

                    // Solo permitir visualización de PDFs en el navegador
                    if (extension == ".pdf")
                    {
                        var fileBytes = System.IO.File.ReadAllBytes(filePath);
                        return File(fileBytes, "application/pdf");
                    }
                    else
                    {
                        // Para Excel, forzar descarga
                        return DescargarDocumento(idDocumento);
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult EliminarDocumento(int idDocumento)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" });
                }

                using (var dbContext = new SIGEPEntities())
                {
                    // Buscar el documento en la base de datos
                    var documento = dbContext.DocumentosTB.FirstOrDefault(d => d.IdDocumento == idDocumento);

                    if (documento == null)
                    {
                        return Json(new { success = false, message = "Documento no encontrado en la base de datos" });
                    }

                    // Guardar la ruta del archivo antes de eliminar el registro
                    string rutaArchivo = documento.RutaArchivo;

                    // Eliminar el registro de la base de datos
                    dbContext.DocumentosTB.Remove(documento);
                    dbContext.SaveChanges();

                    // Eliminar el archivo físico si existe
                    if (!string.IsNullOrEmpty(rutaArchivo) && System.IO.File.Exists(rutaArchivo))
                    {
                        try
                        {
                            System.IO.File.Delete(rutaArchivo);
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

        [HttpGet]
        public JsonResult ObtenerEspecialidades()
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new { success = false, message = "Sesión expirada" }, JsonRequestBehavior.AllowGet);
                }

                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
                int idRol = Convert.ToInt32(Session["IdRol"]);

                using (var db = new SIGEPEntities())
                {
                    List<object> especialidades = new List<object>();

                    if (idRol == 3) // Profesor
                    {
                        var especialidadProfesor = (from ue in db.UsuarioEspecialidadTB
                                                    join e in db.EspecialidadesTB on ue.IdEspecialidad equals e.IdEspecialidad
                                                    where ue.IdUsuario == idUsuario && ue.IdEstado == 1
                                                    select new { e.Nombre })
                                                   .FirstOrDefault();

                        if (especialidadProfesor != null)
                        {
                            especialidades.Add(new { Value = especialidadProfesor.Nombre, Text = especialidadProfesor.Nombre });
                        }
                    }
                    else // Coordinador o Admin
                    {
                        especialidades.Add(new { Value = "", Text = "-- Todas las especialidades --" });

                        var lista = db.EspecialidadesTB
                            .OrderBy(e => e.Nombre)
                            .Select(e => new { Value = e.Nombre, Text = e.Nombre })
                            .ToList();

                        especialidades.AddRange(lista.Cast<object>());
                    }

                    return Json(new { success = true, especialidades = especialidades }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

