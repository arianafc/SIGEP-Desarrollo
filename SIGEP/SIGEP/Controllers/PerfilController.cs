using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class PerfilController : Controller
    {
        [HttpGet]
        [FiltroSesion]
        public ActionResult MiPerfil()
        {
            try
            {
                var Usuario = new Autenticacion();
                using (var dbContext = new SIGEPEntities())
                {
                    var cedula = Session["Cedula"]?.ToString();
                    if (string.IsNullOrEmpty(cedula))
                    {
                        TempData["SwalError"] = "La sesión expiró. Vuelva a iniciar sesión.";
                        return RedirectToAction("Login", "Home");
                    }

                    var usuarioData = dbContext.UsuariosTB.FirstOrDefault(u => u.Cedula == cedula);
                    var usuarioCorreo = dbContext.EmailsTB.FirstOrDefault(e => e.IdUsuario == usuarioData.IdUsuario);

                    if (usuarioData != null)
                    {
                        Usuario.IdUsuario = usuarioData.IdUsuario;
                        Usuario.Cedula = usuarioData.Cedula;
                        Usuario.Nombre = usuarioData.Nombre;
                        Usuario.Apellido1 = usuarioData.Apellido1;
                        Usuario.Apellido2 = usuarioData.Apellido2;
                        Usuario.FechaNacimiento = usuarioData.FechaNacimiento;
                        Usuario.FechaRegistro = usuarioData.FechaRegistro;
                        Usuario.FechaEgreso = usuarioData.FechaEgreso ?? null;
                        Usuario.IdSeccion = usuarioData.IdSeccion ?? 0;
                        Usuario.IdDireccion = usuarioData.IdDireccion ?? 0;
                        Usuario.IdRol = usuarioData.IdRol;
                        Usuario.IdEstado = usuarioData.IdEstado;
                        Usuario.Correo = usuarioCorreo?.Email ?? "";

                        // Cargar listas para dropdowns
                        Usuario.ListaSecciones = dbContext.SeccionesTB.ToList();
                        Usuario.ListaEspecialidades = dbContext.EspecialidadesTB.ToList();
                    }

                    return View(Usuario);
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpPost]
        public ActionResult CambiarContrasenna(Autenticacion usuario)
        {
           try
            {
                if (usuario.Contrasenna == usuario.NuevaContrasenna)
                {
                    using (var dbContext = new SIGEPEntities()) { 
                       var cedula = Session["Cedula"].ToString();
                       var result = dbContext.CambiarContrasennaSP(cedula, usuario.Contrasenna);
                        if (result != 0)
                        {
                            TempData["SwalSuccess"] = "Contraseña actualizada exitosamente.";
                        } else
                        {
                            TempData["SwalError"] = "No se pudo cambiar la contraseña, intente de nuevo.";
                        }

                    }
                } else
                {
                            TempData["SwalError"] = "La nueva contraseña no coincide con la confirmación. Intente de nuevo.";
                }
            } catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }


            return RedirectToAction("MiPerfil");
        }
    }
}