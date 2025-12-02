using Antlr.Runtime.Misc;
using SIGEP.EF;
using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Xml.Linq;

namespace SIGEP.Controllers
{
    public class PerfilController : Controller
    {

        [FiltroSesion]
        [HttpGet]
     
        public ActionResult MiPerfil()
        {
            try
            {
                
                var Usuario = new UsuarioModel();

                using (var dbContext = new SIGEPEntities())
                {

                  


                    // ===============================
                    // VALIDAR SESIÓN
                    // ===============================
                    var cedula = Session["Cedula"]?.ToString();
                    var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
                   

                    if (string.IsNullOrEmpty(cedula))
                    {
                        TempData["SwalError"] = "La sesión expiró. Vuelva a iniciar sesión.";
                        return RedirectToAction("Login", "Home");
                    }

                    // ===============================
                    // OBTENER DATOS PRINCIPALES DEL USUARIO
                    // ===============================
                    var usuarioData = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == IdUsuario);

                    if (usuarioData == null)
                    {
                        TempData["SwalError"] = "No se encontró información del usuario.";
                        return RedirectToAction("Index", "Home");
                    }

                    // ===============================
                    // DATOS RELACIONADOS
                    // ===============================
                    var usuarioCorreos = dbContext.EmailsTB.Where(e => e.IdUsuario == IdUsuario).ToList();
                    var usuarioSeccion = dbContext.SeccionesTB.FirstOrDefault(s => s.IdSeccion == usuarioData.IdSeccion);
                    var usuarioEspecialidad = dbContext.UsuarioEspecialidadTB.FirstOrDefault(es => es.IdUsuario == usuarioData.IdUsuario);
                    var especialidad = usuarioEspecialidad != null
                        ? dbContext.EspecialidadesTB.FirstOrDefault(es => es.IdEspecialidad == usuarioEspecialidad.IdEspecialidad)
                        : null;
                    var InfoMedica = dbContext.InformacionMedicaTB.FirstOrDefault(im => im.IdUsuario == usuarioData.IdUsuario);
                    var direccion = dbContext.DireccionesTB.FirstOrDefault(d => d.IdDireccion == usuarioData.IdDireccion);
                    var InfoAcademica = dbContext.FormacionAcademicaTB.FirstOrDefault(u => u.IdUsuario == usuarioData.IdUsuario);
                    var InfoLaboral = dbContext.InformacionLaboralTB.FirstOrDefault(u => u.IdUsuario == usuarioData.IdUsuario);
                    // ===============================
                    // CORREOS
                    // ===============================
                    var usuarioCorreoMEP = usuarioCorreos.FirstOrDefault(e => e.Email.ToLower().Contains("@mep.go.cr"));
                    var usuarioCorreoPersonal = usuarioCorreos.FirstOrDefault(e => !e.Email.ToLower().Contains("@mep.go.cr"));

                    // ===============================
                    // ENCARGADOS
                    // ===============================
                    var Encargados = dbContext.ObtenerEncargadosUsuarioSP(usuarioData.IdUsuario).ToList();
                    var EncargadoMostrar = Encargados.Where(ee => ee.IdEstado == 1).ToList();

                    // ===============================
                    // DIRECCIÓN COMPLETA (provincia, cantón, distrito)
                    // ===============================
                    if (direccion != null)
                    {
                        var distritoEntity = dbContext.DistritosTB.FirstOrDefault(d => d.IdDistrito == direccion.IdDistrito);
                        Usuario.distrito = distritoEntity?.Nombre ?? "";

                        var cantonEntity = distritoEntity != null
                            ? dbContext.CantonesTB.FirstOrDefault(c => c.IdCanton == distritoEntity.IdCanton)
                            : null;
                        Usuario.canton = cantonEntity?.Nombre ?? "";

                        var provinciaEntity = cantonEntity != null
                            ? dbContext.ProvinciasTB.FirstOrDefault(p => p.IdProvincia == cantonEntity.IdProvincia)
                            : null;
                        Usuario.provincia = provinciaEntity?.Nombre ?? "";
                        Usuario.DireccionExacta = direccion.DireccionExacta ?? "";
                    }


                    // ===============================
                    // INFORMACIÓN GENERAL DEL USUARIO
                    // ===============================
                    Usuario.IdUsuario = usuarioData.IdUsuario;
                    Usuario.Cedula = usuarioData.Cedula;
                    Usuario.Nombre = usuarioData.Nombre;
                    Usuario.Apellido1 = usuarioData.Apellido1;
                    Usuario.Apellido2 = usuarioData.Apellido2;
                    Usuario.FechaNacimiento = usuarioData.FechaNacimiento;
                    Usuario.FechaRegistro = usuarioData.FechaRegistro;
                    Usuario.FechaEgreso = usuarioData.FechaEgreso;
                    Usuario.IdSeccion = usuarioData.IdSeccion ?? 0;
                    Usuario.IdDireccion = usuarioData.IdDireccion ?? 0;
                    Usuario.IdRol = usuarioData.IdRol;
                    Usuario.IdEstado = usuarioData.IdEstado;
                    Usuario.Nacionalidad = usuarioData.Nacionalidad ?? "";
                    Usuario.Sexo = usuarioData.Sexo ?? "";

                 
                    // ===============================
                    // CORREOS
                    // ===============================
                    Usuario.CorreoMEP = usuarioCorreoMEP?.Email ?? "";
                    Usuario.CorreoPersonal = usuarioCorreoPersonal?.Email ?? "";

                    // ===============================
                    // SECCIÓN
                    // ===============================
                    Usuario.NombreSeccion = usuarioSeccion?.Seccion ?? "";

                    // ===============================
                    // ESPECIALIDAD
                    // ===============================
                    Usuario.NombreEspecialidad = especialidad?.Nombre ?? "";
                    Usuario.IdEspecialidad = especialidad?.IdEspecialidad ?? 0;

                    // ===============================
                    // INFORMACIÓN MÉDICA
                    // ===============================
                    Usuario.Padecimiento = InfoMedica?.Padecimiento ?? "";
                    Usuario.Alergia = InfoMedica?.Alergia ?? "";
                    Usuario.Tratamiento = InfoMedica?.Tratamiento ?? "";

                    // ===============================
                    // INFORMACIÓN ACADÉMICA
                    // ===============================
                    Usuario.Carrera = InfoAcademica?.Carrera ?? "";
                    Usuario.TituloObtenido = InfoAcademica?.Titulo ?? "";
                    Usuario.AnnoGraduacion = InfoAcademica?.AnnoGraduacion ?? 0; // tipo INT

                    // ===============================
                    // INFORMACIÓN ACADÉMICA
                    // ===============================
                    Usuario.EmpresaActual = InfoLaboral?.EmpresaActual ?? "";
                    Usuario.PuestoActual = InfoLaboral?.PuestoActual ?? "";

                    // ===============================
                    // TELÉFONO
                    // ===============================
                    Usuario.Telefono = dbContext.TelefonosTB
                        .FirstOrDefault(t => t.IdUsuario == usuarioData.IdUsuario)?.Telefono ?? "";

                    // ===============================
                    // LISTAS PARA DROPDOWNS Y ENCARGADOS
                    // ===============================
                    Usuario.ListaSecciones = dbContext.SeccionesTB.ToList();
                    Usuario.ListaEspecialidades = dbContext.EspecialidadesTB.ToList();

                    var lista = dbContext.UsuarioEspecialidadTB
                        .Where(u => u.IdUsuario == IdUsuario)
                        .Join(
                            dbContext.EspecialidadesTB,
                            u => u.IdEspecialidad,
                            e => e.IdEspecialidad,
                            (u, e) => new
                            {
                                u.IdEspecialidad,
                                u.IdUsuario,
                                u.IdUsuarioEspecialidad,
                                e.Nombre,
                                u.IdEstado
                            }
                        )
                        .ToList();

                    Usuario.ListaEspecialidadesUsuario = lista
                        .Select(enc => new UsuarioEspecialidadModel
                        {
                            IdEspecialidad = enc.IdEspecialidad,
                            IdUsuario = enc.IdUsuario,
                            IdUsuarioEspecialidad = enc.IdUsuarioEspecialidad,
                            Nombre = enc.Nombre  ,
                            IdEstado = enc.IdEstado
                        })
                        .ToList();


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

                    Usuario.ListaEncargadoMostrar = EncargadoMostrar.Select(enc => new EncargadoDTO
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

                    // ===============================
                    // DEVOLVER A LA VISTA
                    // ===============================
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
        public ActionResult ActualizarPerfil(UsuarioModel usuario)
        {
            var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
            var IdRol = Convert.ToInt32(Session["IdRol"]);
            using (var dbContext = new SIGEPEntities())
            {
                var usuarioToUpdate = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == IdUsuario);
                if (usuarioToUpdate != null)
                {
                    // ===============================
                    // Validar cédula
                    // ===============================
                    if (usuarioToUpdate.Cedula != usuario.Cedula)
                    {
                        var cedulaExistente = dbContext.UsuariosTB
                            .FirstOrDefault(u => u.Cedula == usuario.Cedula && u.IdUsuario != IdUsuario);

                        if (cedulaExistente != null)
                        {
                            TempData["SwalError"] = "La cédula ya está en uso por otro usuario.";
                            return Redirect("MiPerfil");
                        }
                        else if (IdRol == 1)
                        {
                            var EsEncargado = dbContext.EncargadosTB.FirstOrDefault(u => u.Cedula == usuario.Cedula);
                            TempData["SwalError"] = "Error: La cédula indicada se encuentra asociada a un encargado";
                            return Redirect("MiPerfil");
                        }
                        usuarioToUpdate.Cedula = usuario.Cedula;
                        Session["cedula"] = usuario.Cedula;
                    }

                    // ===============================
                    // Actualizaciones básicas
                    // ===============================
                    usuarioToUpdate.Nombre = usuario.Nombre;
                    usuarioToUpdate.Apellido1 = usuario.Apellido1;
                    usuarioToUpdate.Apellido2 = usuario.Apellido2;
                    usuarioToUpdate.FechaNacimiento = usuario.FechaNacimiento;
                    usuarioToUpdate.Nacionalidad = usuario.Nacionalidad;
                    usuarioToUpdate.Sexo = usuario.Sexo;

                    // ===============================
                    // Correos
                    // ===============================
                    var correoPersonal = dbContext.EmailsTB
                        .FirstOrDefault(e => e.IdUsuario == IdUsuario && !e.Email.ToLower().Contains("@mep.go.cr"));
                    if (correoPersonal != null)
                        correoPersonal.Email = usuario.CorreoPersonal;
                    else if (!string.IsNullOrEmpty(usuario.CorreoPersonal))
                        dbContext.EmailsTB.Add(new EmailsTB { IdUsuario = IdUsuario, Email = usuario.CorreoPersonal });
                    if (IdRol != 4)
                    {
                        var correoMEP = dbContext.EmailsTB
                       .FirstOrDefault(e => e.IdUsuario == IdUsuario && e.Email.ToLower().Contains("@mep.go.cr"));
                        if (correoMEP != null)
                            correoMEP.Email = usuario.CorreoMEP;
                        else if (!string.IsNullOrEmpty(usuario.CorreoMEP))
                            dbContext.EmailsTB.Add(new EmailsTB { IdUsuario = IdUsuario, Email = usuario.CorreoMEP });
                    }


                    // ===============================
                    // Teléfono
                    // ===============================
                    var telefono = dbContext.TelefonosTB.FirstOrDefault(t => t.IdUsuario == IdUsuario);
                    if (telefono != null)
                        telefono.Telefono = usuario.Telefono;
                    else if (!string.IsNullOrEmpty(usuario.Telefono))
                        dbContext.TelefonosTB.Add(new TelefonosTB { IdUsuario = IdUsuario, Telefono = usuario.Telefono });

                    // ===============================
                    // Provincias / Cantones / Distritos
                    // ===============================
                    var provincia = dbContext.ProvinciasTB.FirstOrDefault(p => p.Nombre == usuario.provincia)
                                    ?? dbContext.ProvinciasTB.Add(new ProvinciasTB { Nombre = usuario.provincia });
                    dbContext.SaveChanges();

                    var canton = dbContext.CantonesTB
                        .FirstOrDefault(c => c.Nombre == usuario.canton && c.IdProvincia == provincia.IdProvincia)
                        ?? dbContext.CantonesTB.Add(new CantonesTB { Nombre = usuario.canton, IdProvincia = provincia.IdProvincia });
                    dbContext.SaveChanges();

                    var distrito = dbContext.DistritosTB
                        .FirstOrDefault(d => d.Nombre == usuario.distrito && d.IdCanton == canton.IdCanton)
                        ?? dbContext.DistritosTB.Add(new DistritosTB { Nombre = usuario.distrito, IdCanton = canton.IdCanton });
                    dbContext.SaveChanges();

                    // ===============================
                    // Dirección del usuario
                    // ===============================
                    DireccionesTB direccion;
                    if (usuarioToUpdate.IdDireccion != null)
                    {
                        // Actualizar dirección existente
                        direccion = dbContext.DireccionesTB.FirstOrDefault(d => d.IdDireccion == usuarioToUpdate.IdDireccion);
                        if (direccion != null)
                        {
                            direccion.IdDistrito = distrito.IdDistrito;
                            direccion.DireccionExacta = usuario.DireccionExacta;
                        }
                    }
                    else
                    {
                        // Insertar nueva dirección
                        direccion = new DireccionesTB
                        {
                            IdDistrito = distrito.IdDistrito,
                            DireccionExacta = usuario.DireccionExacta,
                            IdEstado = 1
                        };
                        dbContext.DireccionesTB.Add(direccion);
                        dbContext.SaveChanges();

                        // Asignar IdDireccion al usuario
                        usuarioToUpdate.IdDireccion = direccion.IdDireccion;
                    }

                    dbContext.SaveChanges();
                    TempData["SwalSuccess"] = "Perfil actualizado exitosamente.";
                }
                else
                {
                    TempData["SwalError"] = "Usuario no encontrado.";
                }

                return Redirect("MiPerfil");
            }
        }


        [HttpPost]
        public ActionResult CambiarContrasenna(UsuarioModel usuario)
        {
            try
            {
                if (usuario.Contrasenna == usuario.NuevaContrasenna)
                {
                    using (var dbContext = new SIGEPEntities())
                    {
                        var cedula = Session["Cedula"].ToString();
                        var result = dbContext.CambiarContrasennaSP(cedula, usuario.Contrasenna);
                        if (result != 0)
                        {
                            TempData["SwalSuccess"] = "Contraseña actualizada exitosamente.";
                        }
                        else
                        {
                            TempData["SwalError"] = "No se pudo cambiar la contraseña, intente de nuevo.";
                        }

                    }
                }
                else
                {
                    TempData["SwalError"] = "La nueva contraseña no coincide con la confirmación. Intente de nuevo.";
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }


            return RedirectToAction("MiPerfil");
        }

        [HttpPost]
        public ActionResult ActualizarEncargado(
            int IdEncargado, string Nombre, string Apellido1, string Apellido2,
            string Telefono, string Parentesco, string LugarTrabajo,
            string Ocupacion, string Correo, string Cedula)
        {
            try
            {
                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var db = new SIGEPEntities())
                {
                    // 1 Validar si la cédula pertenece a un estudiante activo
                    var esEstudiante = (from u in db.UsuariosTB
                                        join r in db.RolesTB on u.IdRol equals r.IdRol
                                        where u.Cedula == Cedula && r.Descripcion == "Estudiante" && u.IdEstado == 1
                                        select u).Any();

                    if (esEstudiante)
                    {
                        return Json(new
                        {
                            success = false,
                            mensaje = "La cédula ingresada pertenece a un estudiante activo de la institución."
                        });
                    }

                    // 2️ Validar si la cédula ya pertenece a otro encargado
                    var duplicado = db.EncargadosTB
                        .FirstOrDefault(e => e.Cedula == Cedula && e.IdEncargado != IdEncargado);

                    if (duplicado != null)
                    {
                        return Json(new
                        {
                            success = false,
                            mensaje = "La cédula ya pertenece a otro encargado. Revise los registros existentes."
                        });
                    }

                    // 3️ Buscar encargado actual
                    var encargado = db.EncargadosTB.FirstOrDefault(e => e.IdEncargado == IdEncargado);
                    if (encargado == null)
                    {
                        return Json(new { success = false, mensaje = "No se encontró el encargado." });
                    }

                    // 4️ Actualizar datos básicos

                    var parentescoExistente = db.EstudianteEncargadoTB.FirstOrDefault(ee => ee.IdEncargado == IdEncargado && ee.IdUsuario == IdUsuario);

                    parentescoExistente.Parentesco = Parentesco;
                    encargado.Cedula = Cedula;
                    encargado.Nombre = Nombre;
                    encargado.Apellido1 = Apellido1;
                    encargado.Apellido2 = Apellido2;
                    encargado.Ocupacion = Ocupacion;
                    encargado.LugarTrabajo = LugarTrabajo;
                    db.SaveChanges();

                    // 5️ Actualizar teléfono
                    var telefonoExistente = db.TelefonosTB.FirstOrDefault(t => t.IdEncargado == IdEncargado);
                    if (telefonoExistente != null)
                        telefonoExistente.Telefono = Telefono;
                    else
                        db.TelefonosTB.Add(new TelefonosTB { Telefono = Telefono, IdEncargado = IdEncargado });

                    // 6️ Actualizar correo
                    var correoExistente = db.EmailsTB.FirstOrDefault(c => c.IdEncargado == IdEncargado);
                    if (correoExistente != null)
                        correoExistente.Email = Correo;
                    else
                        db.EmailsTB.Add(new EmailsTB { Email = Correo, IdEncargado = IdEncargado });

                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        mensaje = "Encargado actualizado exitosamente."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mensaje = "Error interno: " + ex.Message
                });
            }
        }

        [HttpPost]

        public ActionResult AgregarEncargado(string nombre, string apellido1, string apellido2, string telefono, string parentesco, string lugarTrabajo, string ocupacion, string correo, string cedula)
        {
            try
            {

                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var cedulaExiste = (from u in dbContext.UsuariosTB
                                         join r in dbContext.RolesTB on u.IdRol equals r.IdRol
                                         where u.Cedula == cedula && r.Descripcion == "Estudiante" && u.IdEstado == 1
                                         select u).Any();
                    if (cedulaExiste) {

                        return Json(new { success = false, mensaje = "La cédula indicada pertenece a un estudiante activo de la institución." });
                    }

                    var encargadoAsignado = (from e in dbContext.EncargadosTB
                                             join ee in dbContext.EstudianteEncargadoTB on e.IdEncargado equals ee.IdEncargado
                                             where e.Cedula == cedula && ee.IdUsuario == IdUsuario
                                             select e).Any();

                    if (encargadoAsignado) {

                        return Json(new { success = false, mensaje = "La cédula indicada ya pertenece a encargado." });
                    }

                    var accion = 3;
                    var result = dbContext.AccionesEncargadoSP(accion, null, nombre, telefono, parentesco, lugarTrabajo, ocupacion, correo, cedula, apellido1, apellido2, IdUsuario);
                    int? affectedRows = result.FirstOrDefault();

                    if (affectedRows > 0)
                    {

                        return Json(new { success = true, mensaje = "Encargado agregado exitosamente." });

                    }
                    else
                    {
                        return Json(new { success = false, mensaje = "No se pudo agregar el encargado, intente de nuevo." });

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


        [HttpPost]

        public ActionResult ActivarEncargado(int IdEncargado)
        {
            try
            {

                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);

                using (var dbContext = new SIGEPEntities())
                {
                    var accion = 4;
                    var result = dbContext.AccionesEncargadoSP(accion, IdEncargado, "", "", "", "", "", "", "", "", "", IdUsuario);
                    int? affectedRows = result.FirstOrDefault();

                    if (affectedRows > 0)
                    {
                        return Json(new { success = true, mensaje = "Encargado activado exitosamente." });

                    }
                    else
                    {
                        return Json(new { success = false, mensaje = "No se pudo activar el encargado, intente de nuevo." });

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
     .Where(e => e.IdEncargado == idEncargado && e.IdEstado == 1)
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


        [HttpGet]
        public JsonResult ObtenerEncargadoPorCedula(string Cedula)
        {
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var resultado = dbContext.EncargadosTB
                        .FirstOrDefault(e => e.Cedula == Cedula && e.IdEstado == 1);

                    if (resultado == null)
                    {
                        return Json(new
                        {
                            success = false,
                            mensaje = "No se encontró ningún encargado activo con esa cédula."
                        }, JsonRequestBehavior.AllowGet);
                    }

                    var encargado = new EncargadoDTO
                    {
                        IdEncargado = resultado.IdEncargado,
                        Cedula = resultado.Cedula,
                        Nombre = resultado.Nombre,
                        Apellido1 = resultado.Apellido1,
                        Apellido2 = resultado.Apellido2,
                        Ocupacion = resultado.Ocupacion,
                        LugarTrabajo = resultado.LugarTrabajo,
                        Correo = dbContext.EmailsTB
                                    .FirstOrDefault(em => em.IdEncargado == resultado.IdEncargado)?.Email,
                        Telefono = dbContext.TelefonosTB
                                    .FirstOrDefault(t => t.IdEncargado == resultado.IdEncargado)?.Telefono
                    };

                    return Json(new
                    {
                        success = true,
                        data = encargado
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mensaje = "Error interno: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]

        public ActionResult ActualizarInformacionMedica(UsuarioModel usuario)
        {
            try
            {
                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
                using (var dbContext = new SIGEPEntities())
                {
                    var infoMedica = dbContext.InformacionMedicaTB.FirstOrDefault(im => im.IdUsuario == IdUsuario);
                    if (infoMedica != null)
                    {
                        infoMedica.Padecimiento = usuario.Padecimiento;
                        infoMedica.Alergia = usuario.Alergia;
                        infoMedica.Tratamiento = usuario.Tratamiento;
                    }
                    else
                    {
                        dbContext.InformacionMedicaTB.Add(new InformacionMedicaTB
                        {
                            IdUsuario = IdUsuario,
                            Padecimiento = usuario.Padecimiento,
                            Alergia = usuario.Alergia,
                            Tratamiento = usuario.Tratamiento
                        });
                    }
                    dbContext.SaveChanges();
                    TempData["SwalSuccess"] = "Información médica actualizada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("MiPerfil");
        }

        [HttpPost]

        public ActionResult ActualizarEspecialidadSeccion(UsuarioModel usuario)
        {
            try
            {
                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
                using (var dbContext = new SIGEPEntities())
                {
                    var Especialidad = dbContext.UsuarioEspecialidadTB.FirstOrDefault(im => im.IdUsuario == IdUsuario);
                    if (Especialidad != null)
                    {
                        Especialidad.IdEspecialidad = usuario.IdEspecialidad;
                    }
                    else
                    {
                        dbContext.UsuarioEspecialidadTB.Add(new UsuarioEspecialidadTB
                        {
                            IdUsuario = IdUsuario,
                            IdEspecialidad = usuario.IdEspecialidad,
                            IdEstado = 1
                        });

                    }


                    var seccion = dbContext.UsuariosTB.FirstOrDefault(u => u.IdUsuario == IdUsuario);
                    if (seccion != null)
                    {
                        seccion.IdSeccion = usuario.IdSeccion;
                    }

                    dbContext.SaveChanges();
                    TempData["SwalSuccess"] = "Información académica actualizada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("MiPerfil");
        }
            [HttpPost]

            public ActionResult ActualizarInformacionAcademica(UsuarioModel usuario)
            {
                try
                {
                    var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
                    using (var dbContext = new SIGEPEntities())
                    {
                        var InfoAcademica = dbContext.FormacionAcademicaTB.FirstOrDefault(im => im.IdUsuario == IdUsuario);
                        if (InfoAcademica != null)
                        {
                            InfoAcademica.Carrera = usuario.Carrera;
                            InfoAcademica.Titulo = usuario.TituloObtenido;
                            InfoAcademica.AnnoGraduacion = usuario.AnnoGraduacion;
                        }
                        else
                        {
                            dbContext.FormacionAcademicaTB.Add(new FormacionAcademicaTB
                            {
                                Carrera = usuario.Carrera,
                                Titulo = usuario.TituloObtenido,
                                AnnoGraduacion = usuario.AnnoGraduacion,
                                IdUsuario = IdUsuario
                            });

                        }
                        dbContext.SaveChanges();
                        TempData["SwalSuccess"] = "Información académica actualizada exitosamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["SwalError"] = "Error: " + ex.Message;
                }
                return RedirectToAction("MiPerfil");


            }


        [HttpPost]

        public ActionResult ActualizarInformacionLaboral(UsuarioModel usuario)
        {
            try
            {
                var IdUsuario = Convert.ToInt32(Session["IdUsuario"]);
                using (var dbContext = new SIGEPEntities())
                {
                    var InfoLaboral = dbContext.InformacionLaboralTB.FirstOrDefault(im => im.IdUsuario == IdUsuario);
                    if (InfoLaboral != null)
                    {
                        InfoLaboral.EmpresaActual = usuario.EmpresaActual;
                        InfoLaboral.PuestoActual = usuario.PuestoActual;
                    }
                    else
                    {
                        dbContext.InformacionLaboralTB.Add(new InformacionLaboralTB
                        {
                           EmpresaActual = usuario.EmpresaActual,
                            PuestoActual = usuario.PuestoActual,
                            IdUsuario = IdUsuario
                        });

                    }
                    dbContext.SaveChanges();
                    TempData["SwalSuccess"] = "Información laboral actualizada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["SwalError"] = "Error: " + ex.Message;
            }
            return RedirectToAction("MiPerfil");


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
                        return Json(new { success = false, message = "Usuario no encontrado" });
                    }

                    string cedulaEstudiante = estudiante.Cedula;

                    string carpeta = Server.MapPath("~/Documentos/Perfil/" + cedulaEstudiante);

                    // Crear carpeta si no existe
                    if (!Directory.Exists(carpeta))
                    {
                        Directory.CreateDirectory(carpeta);
                    }

                    string nombreOriginal = Path.GetFileNameWithoutExtension(archivo.FileName);
                    string nombreArchivo = $"{cedulaEstudiante}_{nombreOriginal}{extension}";

                    // Ruta FINAL del archivo
                    string ruta = Path.Combine(carpeta, nombreArchivo);

                    // Guardar archivo
                    archivo.SaveAs(ruta);
                    // Guardar registro en BD con la ruta del archivo
                    var documento = new DocumentosTB
                    {
                        Documento = archivo.FileName, // Nombre original para mostrar
                        Tipo = "Perfil",
                        RutaArchivo = "/Documentos/Perfil/" + cedulaEstudiante + "/" + nombreArchivo,
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
        public JsonResult ObtenerDocumentos(int idUsuario)
        {
       
            try
            {
                using (var dbContext = new SIGEPEntities())
                {
                    var documentos = dbContext.ObtenerDocumentosPerfilSP(idUsuario).ToList();

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

        [HttpPost]
        public ActionResult CambioEstadoEspecialidad(int IdUsuarioEspecialidad)
        {
            try
            {
                using (var db = new SIGEPEntities())
                {
                    var especialidad = db.UsuarioEspecialidadTB
                                         .FirstOrDefault(e => e.IdUsuarioEspecialidad == IdUsuarioEspecialidad);

                    if (especialidad == null)
                    {
                        return Json(new
                        {
                            success = false,
                            msg = "No se encontró la especialidad seleccionada."
                        }, JsonRequestBehavior.AllowGet);
                    }

                    if (especialidad.IdEstado == 1)
                    {
                        especialidad.IdEstado = 2;
                        db.SaveChanges();

                        return Json(new
                        {
                            success = true,
                            msg = "Especialidad desactivada correctamente."
                        }, JsonRequestBehavior.AllowGet);
                    }
                    else if (especialidad.IdEstado == 2)
                    {
                        especialidad.IdEstado = 1;
                        db.SaveChanges();

                        return Json(new
                        {
                            success = true,
                            msg = "Especialidad activada correctamente."
                        }, JsonRequestBehavior.AllowGet);
                    }

                 
                    return Json(new
                    {
                        success = false,
                        msg = "El estado actual de la especialidad no es válido para cambiar."
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    msg = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult AgregarEspeciaidad(int IdEspecialidad)
        {
            try
            {
              
                if (Session["IdUsuario"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "La sesión ha expirado. Vuelva a iniciar sesión."
                    }, JsonRequestBehavior.AllowGet);
                }

                int idUsuario;
                if (!int.TryParse(Session["IdUsuario"].ToString(), out idUsuario))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Usuario inválido en sesión."
                    }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new SIGEPEntities())
                {
                 
                    var existeEspecialidad = db.UsuarioEspecialidadTB
                        .FirstOrDefault(e =>
                            e.IdEspecialidad == IdEspecialidad &&
                            e.IdEstado == 1 &&
                            e.IdUsuario == idUsuario);

                    if (existeEspecialidad != null)
                    {
                        return Json(new
                        {
                            success = false,
                            msg = "Lo sentimos. Ya tienes esta especialidad registrada."
                        }, JsonRequestBehavior.AllowGet);
                    }

                 
                    var usuarioEspecialidad = new UsuarioEspecialidadTB
                    {
                        IdEspecialidad = IdEspecialidad,
                        IdUsuario = idUsuario,
                        IdEstado = 1
                    };

                    db.UsuarioEspecialidadTB.Add(usuarioEspecialidad);
                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        msg = "Especialidad agregada con éxito."
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    msg = "Error: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult ActualizarEspecialidad(int IdEspecialidadUsuario, int IdEspecialidad)
        {
            try
            {
                if (Session["IdUsuario"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        msg = "La sesión ha expirado. Vuelva a iniciar sesión."
                    }, JsonRequestBehavior.AllowGet);
                }

                int idUsuario;
                if (!int.TryParse(Session["IdUsuario"].ToString(), out idUsuario))
                {
                    return Json(new
                    {
                        success = false,
                        msg = "Usuario inválido en sesión."
                    }, JsonRequestBehavior.AllowGet);
                }

                using (var db = new SIGEPEntities())
                {
                   
                    var especialidad = db.UsuarioEspecialidadTB
                        .FirstOrDefault(e => e.IdUsuarioEspecialidad == IdEspecialidadUsuario);

                    if (especialidad == null)
                    {
                        return Json(new
                        {
                            success = false,
                            msg = "No se encontró la especialidad a actualizar."
                        }, JsonRequestBehavior.AllowGet);
                    }

                    var especialidadRegistrada = db.UsuarioEspecialidadTB
                        .FirstOrDefault(e =>
                            e.IdEspecialidad == IdEspecialidad &&
                            e.IdEstado == 1 &&
                            e.IdUsuario == idUsuario &&
                            e.IdUsuarioEspecialidad != IdEspecialidadUsuario 
                        );

                    if (especialidadRegistrada != null)
                    {
                        return Json(new
                        {
                            success = false,
                            msg = "Ya existe un registro asociado a esta especialidad."
                        }, JsonRequestBehavior.AllowGet);
                    }

                  
                    especialidad.IdEspecialidad = IdEspecialidad;
                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        msg = "Especialidad actualizada correctamente."
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    msg = "Error: " + ex.Message
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
                    {
                        return HttpNotFound("Documento no encontrado");
                    }

                    // Convertir ruta relativa a física
                    string rutaFisica = Server.MapPath(documento.RutaArchivo);

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









    }





















}

