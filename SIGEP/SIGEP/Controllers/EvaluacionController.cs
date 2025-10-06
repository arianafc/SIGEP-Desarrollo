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
        public JsonResult GuardarNota(int idUsuario, decimal nota1, decimal nota2, decimal notaFinal)
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
                    var resultado = dbContext.GuardarNotaEstudianteSP(
                        idUsuario,
                        nota1,
                        nota2,
                        notaFinal,
                        idProfesor
                    ).FirstOrDefault();

                    if (resultado != null && resultado.Exito == 1)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Nota registrada correctamente"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = resultado?.Mensaje ?? "Error al guardar la nota"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
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

                // Guardar archivo
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{archivo.FileName}";
                var path = System.IO.Path.Combine(Server.MapPath("~/Content/Documentos/Evaluaciones"), fileName);

                // Crear directorio si no existe
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                archivo.SaveAs(path);

                // Guardar registro en BD
                using (var dbContext = new SIGEPEntities())
                {
                    var documento = new DocumentosTB
                    {
                        Documento = archivo.FileName,
                        Tipo = "Evaluación",
                        RutaArchivo = $"/Content/Documentos/Evaluaciones/{fileName}",
                        FechaSubida = DateTime.Now,
                        IdUsuario = idUsuario
                    };

                    dbContext.DocumentosTB.Add(documento);
                    dbContext.SaveChanges();
                }

                return Json(new { success = true, message = "Documento subido correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}

