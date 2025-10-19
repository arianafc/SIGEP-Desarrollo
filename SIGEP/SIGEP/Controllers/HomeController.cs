using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;

namespace SIGEP.Controllers
{
    public class HomeController : Controller
    {

        Utilitarios utilitarios = new Utilitarios();
        [HttpGet]
        [FiltroSesion]
        public ActionResult Index()
        {
            return View();
        }

        #region Login

        [HttpPost]
        public ActionResult Login(UsuarioModel Usuario)

        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var resultado = dbContext.LoginSP(Usuario.Cedula, Usuario.Contrasenna).FirstOrDefault();
                    if (resultado != null)
                    {
                       
                        if(resultado.IdEstado != 1)
                        {
                            return Json(new { success = false, message = "Su cuenta está inactiva. Por favor, contacte al administrador." });
                        } 
                            // Establecer variables de sesión
                        Session["IdRol"] = resultado.IdRol;
                        Session["Nombre"] = resultado.Nombre;
                        Session["Apellido1"] = resultado.Apellido1;
                        Session["Cedula"] = resultado.Cedula;
                        Session["IdUsuario"] = resultado.IdUsuario;
                        Session["Especialidad"] = resultado.Especialidad;

                        return Json(new { success = true, message = "¡Bienvenido a SIGEP, " + resultado.Nombre + "!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Credenciales incorrectas. Por favor, verifica tu cédula o contraseña." });
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = "Error en el servidor, intente más tarde." + e });
            }
        }
       

        [HttpGet]

        public ActionResult Login()
        {
            ViewBag.Mensaje = Session["MensajeError"];
            Session["MensajeError"] = null;
            return View();
        }

        #endregion

        #region Registro

        [HttpGet]
        public ActionResult Registro()
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var Datos = new UsuarioModel();

                    Datos.ListaEspecialidades = dbContext.EspecialidadesTB
                    .Where(e => e.IdEstado == 1)
                                        .ToList();

                    Datos.ListaSecciones = dbContext.SeccionesTB
                        .Where(s => s.IdEstado == 1)
                        .ToList();

                    return View(Datos);
                }
            }
            catch (Exception e)
            {
                TempData["SwalError"] = "Ocurrió un error al cargar el formulario de registro. Por favor, inténtelo de nuevo más tarde.";
                return View();
            }
        }



        [HttpPost]
        public ActionResult Registro(UsuarioModel Usuario)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var resultado = dbContext.RegistroSP(
                        Usuario.Nombre,
                        Usuario.Apellido1,
                        Usuario.Apellido2,
                        Usuario.CorreoPersonal,
                        Usuario.IdEspecialidad,
                        Usuario.FechaNacimiento,
                        Usuario.IdSeccion,
                        Usuario.Contrasenna,
                        Usuario.Cedula
                    ).FirstOrDefault();

                    if (resultado.HasValue && resultado.Value != 0)
                    {
                        return Json(new { success = true, message = "¡Registro exitoso! Ahora puede iniciar sesión." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Ocurrió un error al procesar su registro." });
                    }
                }
            }
            catch (Exception ex)
            {
                
                string mensajeSQL = utilitarios.ObtenerMensajeSQL(ex);

                if (!string.IsNullOrEmpty(mensajeSQL) && mensajeSQL.Contains("Imposible completar el registro. Ya existe una cuenta asociada a esa cédula."))
                {
                    return Json(new { success = false, message = "Imposible completar el registro. Ya existe una cuenta asociada a esa cédula." });
                }

                
                return Json(new { success = false, message = "Ocurrió un error en el servidor, intente más tarde." });
            }
        }

        #endregion

        #region Logout

        [HttpGet]
        public ActionResult Logout()
        {
            return RedirectToAction("Login");
        }

        #endregion

        [HttpGet]

        public ActionResult RecuperarAcceso()
        {
            return View();
        }

        [HttpPost]
        public ActionResult RecuperarAcceso(UsuarioModel usuario)
        {
            using (var dbContext = new SIGEPEntities())
            {
                var result = (from u in dbContext.UsuariosTB
                              join e in dbContext.EmailsTB on u.IdUsuario equals e.IdUsuario
                              where u.Cedula == usuario.Cedula
                              select new
                              {
                                  Usuario = u,
                                  Correo = e.Email
                              }).FirstOrDefault();

                if (result != null)
                {
                    try
                    {
                        var Contrasenna = utilitarios.GenerarPassword();

                        // Cambiar la contraseña en la BD
                        dbContext.CambiarContrasennaSP(usuario.Cedula, Contrasenna);

                        // Construcción del correo
                        StringBuilder mensaje = new StringBuilder();

                        mensaje.Append("<!DOCTYPE html>");
                        mensaje.Append("<html lang='es'>");
                        mensaje.Append("<head><meta charset='UTF-8'></head>");
                        mensaje.Append("<body style='margin:0; padding:0; font-family: Arial, sans-serif; background-color:#f4f4f4;'>");

                        mensaje.Append("<table align='center' width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.1);'>");

                        // Encabezado
                        mensaje.Append("<tr>");
                        mensaje.Append("<td align='center' style='background-color:#2d594d; padding:20px;'>");
                        mensaje.Append("<h2 style='color:#ffffff; margin:0; font-size:22px;'>Recupera Tu Acceso</h2>");
                        mensaje.Append("</td>");
                        mensaje.Append("</tr>");

                        // Contenido
                        mensaje.Append("<tr>");
                        mensaje.Append("<td style='padding:30px; color:#333333; font-size:15px; line-height:1.6;'>");
                        mensaje.Append("Estimado <strong>" + result.Usuario.Nombre + "</strong>,<br><br>");
                        mensaje.Append("Se ha generado una solicitud de recuperación de contraseña a su nombre.<br><br>");
                        mensaje.Append("Su contraseña temporal es: <b style='color:#2d594d; font-size:16px;'>" + Contrasenna + "</b><br><br>");
                        mensaje.Append("Por favor, realice el cambio de su contraseña en cuanto ingrese al sistema.<br><br>");
                        mensaje.Append("Muchas gracias.<br>");
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


                        // Enviar correo
                        if (utilitarios.EnviarCorreo(result.Correo, mensaje.ToString(), "Recuperación de Acceso"))
                        {
                            TempData["SwalSuccess"] = "Se ha enviado un correo con su nueva contraseña. Por favor, revise su bandeja de entrada.";
                        }
                        else
                        {
                            TempData["SwalError"] = "No fue posible enviar el correo de recuperación. Intente más tarde.";
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["SwalError"] = "Ocurrió un error al procesar la solicitud: " + ex.Message;
                    }
                }
                else
                {
                    TempData["SwalError"] = "La cédula ingresada no se encuentra registrada en el sistema.";
                }

                // Siempre redirige a Login
                return RedirectToAction("Login", "Home");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CerrarSesion()
        {
            Session.Abandon();
            Session.Clear();
            return RedirectToAction("Login");
        }

    }
}