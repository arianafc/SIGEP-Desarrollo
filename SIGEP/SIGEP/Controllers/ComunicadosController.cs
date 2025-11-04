using Sigep.Models;            // ComunicadoCardVM
using SIGEP.EF;
using SIGEP.Services;          // Utilitarios (envío de correo + plantilla)
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Sigep.UI.Controllers
{
    public class ComunicadosController : Controller
    {
        private readonly Utilitarios _utils = new Utilitarios();

        // Ajusta si tu contexto EF tiene otro nombre o namespace
        private readonly SIGEPEntities db = new SIGEPEntities();

        /// <summary>
        /// Solo Coordinador (IdRol = 2) puede crear/enviar comunicados.
        /// </summary>
        private bool CanManageComunicados()
        {
            var rol = Session["IdRol"] != null ? Convert.ToInt32(Session["IdRol"]) : 0;
            return rol == 2;
        }

        /// <summary>
        /// Listado de comunicados visible según el rol del usuario (filtro en servidor).
        /// </summary>
        public ActionResult Comunicados()
        {
            if (Session["IdRol"] == null)
                return RedirectToAction("Login", "Home");

            int idRol = Convert.ToInt32(Session["IdRol"]);
            ViewBag.IdRol = idRol;

            // Base: solo comunicados activos
            var q = db.ComunicadosTB.Where(c => c.IdEstado == 1);

            // Armamos la lista de poblaciones permitidas según el rol
            // OJO: estos valores deben coincidir EXACTAMENTE con lo que guardás en c.Poblacion
            // (si usás otras variantes de texto, añadilas aquí)
            var permitidos = new System.Collections.Generic.List<string> { "General" };

            switch (idRol)
            {
                case 2: // Coordinador: ve TODO (no filtramos por población)
                    break;

                case 1: // Estudiante
                    permitidos.Add("Estudiantes");
                    q = q.Where(c => c.Poblacion != null && permitidos.Contains(c.Poblacion));
                    break;

                case 3: // Profesor
                    permitidos.Add("Profesores");
                    q = q.Where(c => c.Poblacion != null && permitidos.Contains(c.Poblacion));
                    break;

                case 4: // Egresado
                    permitidos.Add("Egresados");
                    q = q.Where(c => c.Poblacion != null && permitidos.Contains(c.Poblacion));
                    break;

                default:
                    // Rol inválido: no verá nada
                    q = q.Where(c => 1 == 0);
                    break;
            }

            var model = q
                .OrderByDescending(c => c.Fecha)
                .Select(c => new ComunicadoCardVM
                {
                    Id = c.IdComunicado,
                    Titulo = c.Nombre,
                    FechaPublicacion = c.Fecha,
                    FechaAplicacion = c.FechaLimite,
                    Descripcion = c.Informacion,
                    // Ajusta si tu navegación es distinta
                    PublicadoPor = c.UsuariosTB.Nombre,
                    DirigidoA = c.Poblacion
                })
                .ToList();

            return View(model);
        }


        /// <summary>
        /// Crea un comunicado y notifica por correo a la población objetivo (emails uno por uno).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CrearComunicado(string Titulo, string Descripcion, DateTime? FechaAplicacion, string DirigidoA)
        {
            if (Session["IdRol"] == null) return new HttpStatusCodeResult(401);
            if (!CanManageComunicados()) return new HttpStatusCodeResult(403, "Sin permiso para gestionar comunicados.");

            if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Descripcion) || string.IsNullOrWhiteSpace(DirigidoA))
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                int idUsuarioCreador = 0;
                if (Session["idUsuario"] != null) int.TryParse(Session["idUsuario"].ToString(), out idUsuarioCreador);

                var nuevo = new ComunicadosTB
                {
                    Nombre = Titulo,
                    Informacion = Descripcion,
                    Fecha = DateTime.Now.Date,
                    Poblacion = DirigidoA, // "Estudiantes" | "Profesores" | "Egresados" | "General"
                    FechaLimite = FechaAplicacion,
                    IdUsuario = idUsuarioCreador,
                    IdEstado = 1
                };

                db.ComunicadosTB.Add(nuevo);
                db.SaveChanges();

                // Notificar (uno por uno) a usuarios activos de la población objetivo
                var destinatarios = ObtenerDestinatarios(DirigidoA);
                if (destinatarios.Any())
                {
                    var html = _utils.PlantillaComunicado(Titulo, Descripcion, FechaAplicacion);
                    var asunto = $"[SIGEP] Comunicado - {Titulo}";

                    foreach (var correo in destinatarios)
                        _utils.EnviarCorreo(correo, html, asunto);
                }

                return Json(new { ok = true, msg = "Comunicado publicado y notificado." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = _utils.ObtenerMensajeSQL(ex) ?? "Error al guardar el comunicado." });
            }
        }

        /// <summary>
        /// Envía correo a la población seleccionada (sin crear comunicado).
        /// Adjunta archivo opcional y envía uno por uno (no masivo).
        /// </summary>
        [HttpPost]
        public ActionResult EnviarCorreo(string Poblacion, string Asunto, string Mensaje, HttpPostedFileBase Archivo)
        {
            if (Session["IdRol"] == null) return new HttpStatusCodeResult(401);
            if (!CanManageComunicados()) return new HttpStatusCodeResult(403, "Sin permiso para enviar correos.");

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

                var html = _utils.PlantillaComunicado(Asunto, Mensaje, null);

                foreach (var correo in destinatarios)
                {
                    System.Net.Mail.Attachment adj = null;
                    if (adjBytes != null)
                        adj = new System.Net.Mail.Attachment(new MemoryStream(adjBytes), adjFilename, adjMediaType);

                    _utils.EnviarCorreo(correo, html, Asunto, adj);
                }

                return Json(new { ok = true, msg = "Correos enviados exitosamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = _utils.ObtenerMensajeSQL(ex) ?? "Error al enviar correos." });
            }
        }

        /// <summary>
        /// Devuelve correos de usuarios activos según población:
        /// Profesores: general; Estudiantes: general + especialidades activas; Egresados: general; General: todos activos.
        /// </summary>
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
