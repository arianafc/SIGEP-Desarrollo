using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    //[FiltroSesion]
    //[FiltroUsuarioAdmin]
    public class EstudianteController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();

        // ==============================
        // VISTA PRINCIPAL ESTUDIANTES
        // ==============================
        [HttpGet]
        public ActionResult Estudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Estados = ObtenerEstados();

            return View();
        }

        // ==============================
        // LISTADO ESTUDIANTES (JSON para DataTable)
        // ==============================
        [HttpGet]
        public JsonResult GetEstudiantes(string estado = "", int idEspecialidad = 0)
        {
            var query =
                from u in db.UsuariosTB
                join e in db.EstadosTB on u.IdEstado equals e.IdEstado into je
                from e in je.DefaultIfEmpty()
                select new EstudianteDTO
                {
                    IdUsuario = u.IdUsuario,
                    Cedula = u.Cedula,
                    NombreCompleto = u.Nombre + " " + u.Apellido1 + " " + u.Apellido2,

                    // Teléfono desde TelefonosTB
                    Telefono = db.TelefonosTB
                                 .Where(t => t.IdUsuario == u.IdUsuario)
                                 .OrderBy(t => t.IdTelefono)
                                 .Select(t => t.Telefono)
                                 .FirstOrDefault(),

                    // Especialidad desde UsuarioEspecialidadTB
                    IdEspecialidad = db.UsuarioEspecialidadTB
                                       .Where(ue => ue.IdUsuario == u.IdUsuario)
                                       .OrderByDescending(ue => ue.IdUsuarioEspecialidad)
                                       .Select(ue => ue.IdEspecialidad)
                                       .FirstOrDefault(),

                    EspecialidadNombre =
                        (from ue in db.UsuarioEspecialidadTB
                         join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                         where ue.IdUsuario == u.IdUsuario
                         orderby ue.IdUsuarioEspecialidad descending
                         select esp.Nombre).FirstOrDefault(),

                    IdEstado = u.IdEstado,
                    EstadoNombre = e != null ? e.Descripcion : "",

                    // Estado de práctica desde PracticaEstudianteTB + EstadosTB
                    EstadoPractica =
                        (from p in db.PracticaEstudianteTB
                         join ep in db.EstadosTB on p.IdEstado equals ep.IdEstado
                         where p.IdUsuario == u.IdUsuario
                         orderby p.IdPractica descending
                         select ep.Descripcion).FirstOrDefault()
                };

            // ==============================
            // FILTROS
            // ==============================
            if (!string.IsNullOrEmpty(estado))
            {
                if (estado == "ConPractica")
                    query = query.Where(x => !string.IsNullOrEmpty(x.EstadoPractica));
                else if (estado == "SinPractica")
                    query = query.Where(x => string.IsNullOrEmpty(x.EstadoPractica));
                else
                    query = query.Where(x => x.EstadoNombre == estado);
            }

            if (idEspecialidad > 0)
                query = query.Where(x => x.IdEspecialidad == idEspecialidad);

            // ==============================
            // FILTRO POR ROL
            // ==============================
            var rolUsuario = Session["Rol"] != null ? Session["Rol"].ToString() : "";

            if (rolUsuario == "Profesor")
            {
                int idUsuario = Convert.ToInt32(Session["IdUsuario"]);
                int? idEspecialidadProfesor = db.UsuarioEspecialidadTB
                                                .Where(ue => ue.IdUsuario == idUsuario)
                                                .Select(ue => ue.IdEspecialidad)
                                                .FirstOrDefault();

                if (idEspecialidadProfesor.HasValue)
                    query = query.Where(x => x.IdEspecialidad == idEspecialidadProfesor.Value);
            }
            // Coordinador ve todos (no se filtra)

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
                x.EstadoNombre,
                EstadoPractica = string.IsNullOrEmpty(x.EstadoPractica) ? "No Asignada" : x.EstadoPractica
            }).ToList();

            return Json(new { data = outList }, JsonRequestBehavior.AllowGet);
        }

        // ==============================
        // DETALLE DE ESTUDIANTE
        // ==============================
        [HttpGet]
        public ActionResult Detalle(int id)
        {
            // obtenemos los datos base del usuario (dirección incluida si existe)
            var baseInfo = (from u in db.UsuariosTB
                            join d in db.DireccionesTB on u.IdDireccion equals d.IdDireccion into jd
                            from d in jd.DefaultIfEmpty()
                            where u.IdUsuario == id
                            select new
                            {
                                u.IdUsuario,
                                u.Cedula,
                                u.Nombre,
                                u.Apellido1,
                                u.Apellido2,
                                FechaNacimiento = u.FechaNacimiento, // en tu BD es NOT NULL (DateTime)
                                DireccionExacta = d != null ? d.DireccionExacta : null
                            }).FirstOrDefault();

            if (baseInfo == null)
                return HttpNotFound();

            // correo -> EmailsTB (puede haber varios, tomamos el primero si existe)
            var correo = db.EmailsTB
                          .Where(e => e.IdUsuario == id)
                          .Select(e => e.Email)
                          .FirstOrDefault();

            // teléfono -> TelefonosTB (tomamos el primero si existe)
            var telefono = db.TelefonosTB
                            .Where(t => t.IdUsuario == id)
                            .Select(t => t.Telefono)
                            .FirstOrDefault();

            // especialidad -> UsuarioEspecialidadTB + EspecialidadesTB (última asignada)
            var especialidad = (from ue in db.UsuarioEspecialidadTB
                                join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad
                                where ue.IdUsuario == id
                                orderby ue.IdUsuarioEspecialidad descending
                                select esp.Nombre).FirstOrDefault();

            // estado de la práctica -> PracticaEstudianteTB + EstadosTB (último registro)
            var estadoPractica = (from p in db.PracticaEstudianteTB
                                  join es in db.EstadosTB on p.IdEstado equals es.IdEstado
                                  where p.IdUsuario == id
                                  orderby p.IdPractica descending
                                  select es.Descripcion).FirstOrDefault();

            // calcular edad correctamente con FechaNacimiento (no es nullable según tu script)
            var hoy = DateTime.Today;
            var nacimiento = baseInfo.FechaNacimiento;
            var edad = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-edad)) edad--; // corrige si aún no cumplió este año

            // Mapear a DTO fuertemente tipado
            var estudiante = new EstudianteDetalleDTO
            {
                IdUsuario = baseInfo.IdUsuario,
                Cedula = baseInfo.Cedula,
                Nombre = baseInfo.Nombre,
                Apellido1 = baseInfo.Apellido1,
                Apellido2 = baseInfo.Apellido2,
                Edad = edad,
                Correo = correo ?? string.Empty,
                Telefono = telefono ?? string.Empty,
                Especialidad = especialidad ?? string.Empty,
                Direccion = baseInfo.DireccionExacta ?? string.Empty,
                EstadoPractica = estadoPractica ?? string.Empty
            };

            return View(estudiante);
        }

        // ==============================
        // MÉTODOS AUXILIARES
        // ==============================
        private object ObtenerEspecialidades()
        {
            return db.EspecialidadesTB
                     .Select(x => new { x.IdEspecialidad, x.Nombre })
                     .OrderBy(x => x.Nombre)
                     .ToList();
        }

        private object ObtenerEstados()
        {
            return db.EstadosTB
                     .Select(x => new { x.IdEstado, x.Descripcion })
                     .OrderBy(x => x.Descripcion)
                     .ToList();
        }


        //Actualizar estado
        [HttpPost]
        public JsonResult ActualizarEstado(int idUsuario, int nuevoEstadoId)
        {
            try
            {
                var usuario = db.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);
                if (usuario == null)
                    return Json(new { success = false, message = "Estudiante no encontrado" });

                usuario.IdEstado = nuevoEstadoId;
                db.SaveChanges();

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar: " + ex.Message });
            }
        }

    }

}
