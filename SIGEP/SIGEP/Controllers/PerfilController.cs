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
                var Usuario = new UsuarioModel();
                using (var dbContext = new SIGEPEntities())
                {




                    var cedula = Session["Cedula"]?.ToString();
                    if (string.IsNullOrEmpty(cedula))
                    {
                        TempData["SwalError"] = "La sesión expiró. Vuelva a iniciar sesión.";
                        return RedirectToAction("Login", "Home");
                    }

                    var usuarioData = dbContext.UsuariosTB.FirstOrDefault(u => u.Cedula == cedula);
                    var usuarioCorreos = dbContext.EmailsTB
    .Where(e => e.IdUsuario == usuarioData.IdUsuario)
    .ToList();
                    var usuarioSeccion = dbContext.SeccionesTB.FirstOrDefault(s => s.IdSeccion == usuarioData.IdSeccion);
                    var usuarioEspecialidad = dbContext.UsuarioEspecialidadTB.FirstOrDefault(es => es.IdUsuario == usuarioData.IdUsuario);
                    var especialidad = dbContext.EspecialidadesTB.FirstOrDefault(es => es.IdEspecialidad == usuarioEspecialidad.IdEspecialidad);
                    var InfoMedica = dbContext.InformacionMedicaTB.FirstOrDefault(im => im.IdUsuario == usuarioData.IdUsuario);
                    var direccion = dbContext.DireccionesTB.FirstOrDefault(d => d.IdDireccion == usuarioData.IdDireccion);
                    // Correo institucional (MEP)
                    var usuarioCorreoMEP = usuarioCorreos
                        .FirstOrDefault(e => e.Email.ToLower().Contains("@mep.go.cr"));

                    // Correo personal 
                    var usuarioCorreoPersonal = usuarioCorreos
                        .FirstOrDefault(e => !e.Email.ToLower().Contains("@mep.go.cr"));

                    var Encargados = dbContext.ObtenerEncargadosUsuarioSP(usuarioData.IdUsuario).ToList();



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
                        Usuario.CorreoPersonal = usuarioCorreoPersonal?.Email ?? "";
                        Usuario.CorreoMEP = usuarioCorreoMEP?.Email ?? "";
                        Usuario.NombreEspecialidad = especialidad.Nombre;
                        Usuario.NombreSeccion = usuarioSeccion?.Seccion;
                        Usuario.Padecimiento = InfoMedica?.Padecimiento ?? "N/A";
                        Usuario.Alergia = InfoMedica?.Alergia ?? "N/A";
                        Usuario.Tratamiento = InfoMedica?.Tratamiento ?? "N/A";
                        Usuario.Nacionalidad = usuarioData.Nacionalidad ?? "N/A";
                        Usuario.Sexo = usuarioData.Sexo ?? "N/A";
                        Usuario.DireccionExacta = direccion?.DireccionExacta ?? "N/A";
                        // Cargar listas para dropdowns
                        Usuario.ListaSecciones = dbContext.SeccionesTB.ToList();
                        Usuario.ListaEspecialidades = dbContext.EspecialidadesTB.ToList();
                       Usuario.ListaEncargados = Encargados.Select(enc => new EncargadoDTO
                        {
                            IdEncargado = enc.IdEncargado,
                            Nombre = enc.Nombre,
                            Telefono = enc.Telefono,
                            Parentesco = enc.Parentesco,
                            LugarTrabajo = enc.LugarTrabajo,
                            Ocupacion = enc.Ocupacion,
                            Correo = enc.Correo,
                            Cedula = enc.Cedula,
                            FechaRegistro = enc.FechaRegistro,
                            IdEstado = enc.IdEstado
                        }).ToList();
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
        public ActionResult CambiarContrasenna(UsuarioModel usuario)
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


        [HttpPost]

        public ActionResult ActualizarEncargado(int IdEncargado, string Nombre, string Apellido1, string Apellido2, string Telefono, string Parentesco, string LugarTrabajo, string Ocupacion, string Correo, string Cedula)
        {
            try
            {

                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var accion = 1;
                    var result = dbContext.AccionesEncargadoSP(accion, IdEncargado, Nombre, Telefono, Parentesco, LugarTrabajo, Ocupacion, Correo, Cedula, Apellido1, Apellido2, IdUsuario);
                  int? affectedRows = result.FirstOrDefault();
                    if (affectedRows > 0)
                    {
                        return Json(new { success = true, mensaje = "Encargado actualizado exitosamente." });
                       
                    }
                    else
                    {
                        return Json(new { success = false, mensaje = "No se pudo actualizar el encargado, intente de nuevo." });
                       
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("MiPerfil");
        }


        [HttpPost]

        public ActionResult EliminarEncargado(int IdEncargado)
        {
            try
            {

                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var accion = 2;
                    var result = dbContext.AccionesEncargadoSP(accion, IdEncargado, "", "", "", "", "", "", "", "", "", IdUsuario);
                    int? affectedRows = result.FirstOrDefault();
                    if (affectedRows > 0)
                    {
                        return Json(new { success = true, mensaje = "Encargado desactivado exitosamente." });

                    }
                    else
                    {
                        return Json(new { success = false, mensaje = "No se pudo desactivar el encargado, intente de nuevo." });

                    }
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("MiPerfil");
        }
        

        [HttpGet]
        public JsonResult ObtenerEncargadoPorId(int idEncargado)
        {
            try
            {
                var idUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var db = new SIGEPEntities())
                {
                    // Llama al SP
                    var encargados = db.ObtenerEncargadosUsuarioSP(idUsuario).ToList();

                    // Busca el encargado específico
                    var encargado = encargados
                        .Where(e => e.IdEncargado == idEncargado)
                        .Select(e => new
                        {
                            e.IdEncargado,
                            e.Cedula,
                            e.Nombre,
                            e.Apellido1,
                            e.Apellido2,
                            e.Telefono,
                            e.Parentesco,
                            e.LugarTrabajo,
                            e.Ocupacion,
                            e.Correo
                        })
                        .FirstOrDefault();

                    return Json(encargado, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, mensaje = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}