using SIGEP.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    public class PracticasController : Controller
    {
        private readonly string _cn = ConfigurationManager
            .ConnectionStrings["Sigep"].ConnectionString;

        // GET: VacantesEstudiantes
        [HttpGet]
        public ActionResult VacantesEstudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Modalidades = ObtenerModalidades();
            return View();
        }

        // Listado de vacantes filtrable
        [HttpGet]
        public JsonResult GetVacantes(string estado = "", int idEspecialidad = 0, int idModalidad = 0)
        {
            var list = new List<Vacante>();

            using (var con = new SqlConnection(_cn))
            using (var cmd = new SqlCommand(@"
        SELECT v.IdVacante,
               v.Nombre,
               v.IdEmpresa,
               ISNULL(e.NombreEmpresa,'') AS EmpresaNombre,
               v.Requerimientos,
               v.FechaMaxAplicacion,
               ISNULL(v.NumCupos,0) AS NumCupos,
               v.FechaCierre,
               v.IdModalidad,
               ISNULL(m.Descripcion,'') AS ModalidadNombre,
               v.Descripcion,
               ISNULL(ev.IdEspecialidad,0) AS IdEspecialidad,
               ISNULL(sp.Nombre,'') AS EspecialidadNombre,       -- <- CORREGIDO
               v.IdEstado,
               ISNULL(es.Descripcion,'') AS EstadoNombre,         -- <- CORREGIDO
               ISNULL(p.Postulados,0) AS EstudiantesPostulados
        FROM dbo.VacantesPracticasTB v
        LEFT JOIN dbo.EmpresasTB e  ON e.IdEmpresa = v.IdEmpresa
        LEFT JOIN dbo.EstadosTB es  ON es.IdEstado = v.IdEstado
        LEFT JOIN dbo.EspecialidadesVacantesTB ev ON ev.IdVacante = v.IdVacante
        LEFT JOIN dbo.EspecialidadesTB sp ON sp.IdEspecialidad = ev.IdEspecialidad
        LEFT JOIN dbo.ModalidadesTB m ON m.IdModalidad = v.IdModalidad
        LEFT JOIN (
            SELECT IdVacante, COUNT(1) AS Postulados
            FROM dbo.PracticaEstudianteTB       -- <- CORREGIDO (era PostulacionesTB)
            GROUP BY IdVacante
        ) p ON p.IdVacante = v.IdVacante
        WHERE (@estado = '' OR es.Descripcion = @estado)   -- <- usa la columna correcta para comparar
          AND (@idEspecialidad = 0 OR ev.IdEspecialidad = @idEspecialidad)
          AND (@idModalidad = 0 OR v.IdModalidad = @idModalidad)
        ORDER BY v.IdVacante DESC;", con))
            {
                cmd.Parameters.AddWithValue("@estado", (object)estado ?? "");
                cmd.Parameters.AddWithValue("@idEspecialidad", idEspecialidad);
                cmd.Parameters.AddWithValue("@idModalidad", idModalidad);

                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Vacante
                        {
                            IdVacante = Convert.ToInt32(rd["IdVacante"]),
                            Nombre = rd["Nombre"] as string,
                            IdEmpresa = Convert.ToInt32(rd["IdEmpresa"]),
                            EmpresaNombre = rd["EmpresaNombre"] as string,
                            Requerimientos = rd["Requerimientos"] as string,
                            FechaMaxAplicacion = rd["FechaMaxAplicacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaMaxAplicacion"]),
                            NumCupos = Convert.ToInt32(rd["NumCupos"]),
                            FechaCierre = rd["FechaCierre"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaCierre"]),
                            IdModalidad = rd["IdModalidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdModalidad"]),
                            ModalidadNombre = rd["ModalidadNombre"] as string,
                            Descripcion = rd["Descripcion"] as string,
                            IdEspecialidad = rd["IdEspecialidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdEspecialidad"]),
                            EspecialidadNombre = rd["EspecialidadNombre"] as string,
                            IdEstado = Convert.ToInt32(rd["IdEstado"]),
                            EstadoNombre = rd["EstadoNombre"] as string,
                            EstudiantesPostulados = Convert.ToInt32(rd["EstudiantesPostulados"])
                        });
                    }
                }
            }

            return Json(new { data = list }, JsonRequestBehavior.AllowGet);
        }

   //     // Listado de vacantes filtrable
   //     [HttpGet]
   //     public JsonResult GetVacantes(string estado = "", int idEspecialidad = 0, int idModalidad = 0)
   //     {
   //         var list = new List<Vacante>();

   //         using (var con = new SqlConnection(_cn))
   //         using (var cmd = new SqlCommand(@"
   //         SELECT v.IdVacante,
   //       v.Nombre,
   //       v.IdEmpresa,
   //       ISNULL(e.NombreEmpresa,'') AS EmpresaNombre,
   //       v.Requerimientos,
   //       v.FechaMaxAplicacion,
   //       ISNULL(v.NumCupos,0) AS NumCupos,
   //       v.FechaCierre,
   //       v.IdModalidad,
   //       ISNULL(m.Descripcion,'') AS ModalidadNombre,
   //       v.Descripcion,
   //       ISNULL(ev.IdEspecialidad,0) AS IdEspecialidad,
   //       ISNULL(sp.NombreEspecialidad,'') AS EspecialidadNombre,
   //       v.IdEstado,
   //       ISNULL(es.NombreEstado,'') AS EstadoNombre,
   //       ISNULL(p.Postulados,0) AS EstudiantesPostulados
   //FROM dbo.VacantesPracticasTB v
   //LEFT JOIN dbo.EmpresasTB e  ON e.IdEmpresa = v.IdEmpresa
   //LEFT JOIN dbo.EstadosTB es  ON es.IdEstado = v.IdEstado
   //LEFT JOIN dbo.EspecialidadesVacantesTB ev ON ev.IdVacante = v.IdVacante
   //LEFT JOIN dbo.EspecialidadesTB sp ON sp.IdEspecialidad = ev.IdEspecialidad
   //LEFT JOIN dbo.ModalidadesTB m ON m.IdModalidad = v.IdModalidad
   //LEFT JOIN (
   //    SELECT IdVacante, COUNT(1) AS Postulados
   //    FROM dbo.PostulacionesTB
   //    GROUP BY IdVacante
   //) p ON p.IdVacante = v.IdVacante
   //WHERE (@estado = '' OR es.NombreEstado = @estado)
   //  AND (@idEspecialidad = 0 OR ev.IdEspecialidad = @idEspecialidad)
   //  AND (@idModalidad = 0 OR v.IdModalidad = @idModalidad)
   //ORDER BY v.IdVacante DESC;", con)) 
   //         {
   //             cmd.Parameters.AddWithValue("@estado", (object)estado ?? "");
   //             cmd.Parameters.AddWithValue("@idEspecialidad", idEspecialidad);
   //             cmd.Parameters.AddWithValue("@idModalidad", idModalidad);

   //             con.Open();
   //             using (var rd = cmd.ExecuteReader())
   //             {
   //                 while (rd.Read())
   //                 {
   //                     list.Add(new Vacante
   //                     {
   //                         IdVacante = Convert.ToInt32(rd["IdVacante"]),
   //                         Nombre = rd["Nombre"] as string,
   //                         IdEmpresa = Convert.ToInt32(rd["IdEmpresa"]),
   //                         EmpresaNombre = rd["EmpresaNombre"] as string,
   //                         Requerimientos = rd["Requerimientos"] as string,
   //                         FechaMaxAplicacion = rd["FechaMaxAplicacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaMaxAplicacion"]),
   //                         NumCupos = Convert.ToInt32(rd["NumCupos"]),
   //                         FechaCierre = rd["FechaCierre"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaCierre"]),
   //                         IdModalidad = rd["IdModalidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdModalidad"]),
   //                         ModalidadNombre = rd["ModalidadNombre"] as string,
   //                         Descripcion = rd["Descripcion"] as string,
   //                         IdEspecialidad = rd["IdEspecialidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdEspecialidad"]),
   //                         EspecialidadNombre = rd["EspecialidadNombre"] as string,
   //                         IdEstado = Convert.ToInt32(rd["IdEstado"]),
   //                         EstadoNombre = rd["EstadoNombre"] as string,
   //                         EstudiantesPostulados = Convert.ToInt32(rd["EstudiantesPostulados"])
   //                     });
   //                 }
   //             }
   //         }
   //         using (var con = new SqlConnection(_cn))
   //         using (var cmd = new SqlCommand(@"
   //         SELECT v.IdVacante,
   //       v.Nombre,
   //       v.IdEmpresa,
   //       ISNULL(e.NombreEmpresa,'') AS EmpresaNombre,
   //       v.Requerimientos,
   //       v.FechaMaxAplicacion,
   //       ISNULL(v.NumCupos,0) AS NumCupos,
   //       v.FechaCierre,
   //       v.IdModalidad,
   //       ISNULL(m.Descripcion,'') AS ModalidadNombre,
   //       v.Descripcion,
   //       ISNULL(ev.IdEspecialidad,0) AS IdEspecialidad,
   //       ISNULL(sp.NombreEspecialidad,'') AS EspecialidadNombre,
   //       v.IdEstado,
   //       ISNULL(es.NombreEstado,'') AS EstadoNombre,
   //       ISNULL(p.Postulados,0) AS EstudiantesPostulados
   //FROM dbo.VacantesPracticasTB v
   //LEFT JOIN dbo.EmpresasTB e  ON e.IdEmpresa = v.IdEmpresa
   //LEFT JOIN dbo.EstadosTB es  ON es.IdEstado = v.IdEstado
   //LEFT JOIN dbo.EspecialidadesVacantesTB ev ON ev.IdVacante = v.IdVacante
   //LEFT JOIN dbo.EspecialidadesTB sp ON sp.IdEspecialidad = ev.IdEspecialidad
   //LEFT JOIN dbo.ModalidadesTB m ON m.IdModalidad = v.IdModalidad
   //LEFT JOIN (
   //    SELECT IdVacante, COUNT(1) AS Postulados
   //    FROM dbo.PostulacionesTB
   //    GROUP BY IdVacante
   //) p ON p.IdVacante = v.IdVacante
   //WHERE (@estado = '' OR es.NombreEstado = @estado)
   //  AND (@idEspecialidad = 0 OR ev.IdEspecialidad = @idEspecialidad)
   //  AND (@idModalidad = 0 OR v.IdModalidad = @idModalidad)
   //ORDER BY v.IdVacante DESC;", con))   // 👈 aquí cerramos bien el SQL y el )
   //         {
   //             cmd.Parameters.AddWithValue("@estado", (object)estado ?? "");
   //             cmd.Parameters.AddWithValue("@idEspecialidad", idEspecialidad);
   //             cmd.Parameters.AddWithValue("@idModalidad", idModalidad);

   //             con.Open();
   //             using (var rd = cmd.ExecuteReader())
   //             {
   //                 while (rd.Read())
   //                 {
   //                     list.Add(new Vacante
   //                     {
   //                         IdVacante = Convert.ToInt32(rd["IdVacante"]),
   //                         Nombre = rd["Nombre"] as string,
   //                         IdEmpresa = Convert.ToInt32(rd["IdEmpresa"]),
   //                         EmpresaNombre = rd["EmpresaNombre"] as string,
   //                         Requerimientos = rd["Requerimientos"] as string,
   //                         FechaMaxAplicacion = rd["FechaMaxAplicacion"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaMaxAplicacion"]),
   //                         NumCupos = Convert.ToInt32(rd["NumCupos"]),
   //                         FechaCierre = rd["FechaCierre"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rd["FechaCierre"]),
   //                         IdModalidad = rd["IdModalidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdModalidad"]),
   //                         ModalidadNombre = rd["ModalidadNombre"] as string,
   //                         Descripcion = rd["Descripcion"] as string,
   //                         IdEspecialidad = rd["IdEspecialidad"] == DBNull.Value ? 0 : Convert.ToInt32(rd["IdEspecialidad"]),
   //                         EspecialidadNombre = rd["EspecialidadNombre"] as string,
   //                         IdEstado = Convert.ToInt32(rd["IdEstado"]),
   //                         EstadoNombre = rd["EstadoNombre"] as string,
   //                         EstudiantesPostulados = Convert.ToInt32(rd["EstudiantesPostulados"])
   //                     });
   //                 }
   //             }
   //         }

   //         return Json(new { data = list
   // }, JsonRequestBehavior.AllowGet);
   //     }

        [HttpPost]
        public JsonResult Crear(Vacante v)
        {
            var errores = ValidarCrearEditar(v);
            if (errores.Count > 0)
                return Json(new { ok = false, message = string.Join(" | ", errores) });

            const int IdEstadoNoAsignada = 2;

            using (var con = new SqlConnection(_cn))
            using (var cmd = new SqlCommand(@"
        INSERT INTO dbo.VacantesPracticasTB
            (Nombre, IdEmpresa, Requerimientos, FechaMaxAplicacion, NumCupos, 
             FechaCierre, IdModalidad, Descripcion, IdEstado)
        VALUES
            (@Nombre, @IdEmpresa, @Requerimientos, @FechaMaxAplicacion, @NumCupos, 
             @FechaCierre, @IdModalidad, @Descripcion, @IdEstado);
        SELECT SCOPE_IDENTITY();", con))
            {
                cmd.Parameters.AddWithValue("@Nombre", v.Nombre?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@IdEmpresa", v.IdEmpresa);
                cmd.Parameters.AddWithValue("@Requerimientos", (object)v.Requerimientos ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaMaxAplicacion", (object)v.FechaMaxAplicacion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@NumCupos", v.NumCupos);
                cmd.Parameters.AddWithValue("@FechaCierre", (object)v.FechaCierre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdModalidad", v.IdModalidad);
                cmd.Parameters.AddWithValue("@Descripcion", (object)v.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdEstado", IdEstadoNoAsignada);

                con.Open();
                var newId = Convert.ToInt32(cmd.ExecuteScalar());

                //Ahora insertar la especialidad en la tabla puente
                using (var cmdEsp = new SqlCommand(@"
            INSERT INTO dbo.EspecialidadesVacantesTB (IdEspecialidad, IdVacante)
            VALUES (@IdEspecialidad, @IdVacante);", con))
                {
                    cmdEsp.Parameters.AddWithValue("@IdEspecialidad", v.IdEspecialidad);
                    cmdEsp.Parameters.AddWithValue("@IdVacante", newId);
                    cmdEsp.ExecuteNonQuery();
                }

                return Json(new { ok = true, id = newId, message = "Vacante Registrada Exitosamente" });
            }
        }

        [HttpPost]
        public JsonResult Editar(Vacante v)
        {
            if (v == null || v.IdVacante <= 0)
                return Json(new { ok = false, message = "Id de vacante inválido." });

            var errores = ValidarCrearEditar(v);
            if (errores.Count > 0)
                return Json(new { ok = false, message = string.Join(" | ", errores) });

            using (var con = new SqlConnection(_cn))
            {
                con.Open();

                // Primero actualizamos la vacante
                using (var cmd = new SqlCommand(@"
            UPDATE dbo.VacantesPracticasTB
               SET Nombre             = @Nombre,
                   IdEmpresa          = @IdEmpresa,
                   Requerimientos     = @Requerimientos,
                   FechaMaxAplicacion = @FechaMaxAplicacion,
                   NumCupos           = @NumCupos,
                   FechaCierre        = @FechaCierre,
                   IdModalidad        = @IdModalidad,
                   Descripcion        = @Descripcion,
                   IdEstado           = @IdEstado
             WHERE IdVacante = @IdVacante;", con))
                {
                    cmd.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                    cmd.Parameters.AddWithValue("@Nombre", v.Nombre?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@IdEmpresa", v.IdEmpresa);
                    cmd.Parameters.AddWithValue("@Requerimientos", (object)v.Requerimientos ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FechaMaxAplicacion", (object)v.FechaMaxAplicacion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@NumCupos", v.NumCupos);
                    cmd.Parameters.AddWithValue("@FechaCierre", (object)v.FechaCierre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdModalidad", v.IdModalidad);
                    cmd.Parameters.AddWithValue("@Descripcion", (object)v.Descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IdEstado", v.IdEstado);

                    var rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return Json(new { ok = false, message = "No se encontró la vacante." });
                }

                // actualizar la especialidad en la tabla puente
                using (var cmdDel = new SqlCommand("DELETE FROM dbo.EspecialidadesVacantesTB WHERE IdVacante = @IdVacante", con))
                {
                    cmdDel.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                    cmdDel.ExecuteNonQuery();
                }

                using (var cmdIns = new SqlCommand(@"
            INSERT INTO dbo.EspecialidadesVacantesTB (IdEspecialidad, IdVacante)
            VALUES (@IdEspecialidad, @IdVacante);", con))
                {
                    cmdIns.Parameters.AddWithValue("@IdEspecialidad", v.IdEspecialidad);
                    cmdIns.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                    cmdIns.ExecuteNonQuery();
                }

                return Json(new { ok = true, message = "Práctica actualizada correctamente" });
            }
        }

        //eliminar vacante
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            if (id <= 0)
                return Json(new { ok = false, message = "Id inválido" });

            using (var con = new SqlConnection(_cn))
            {
                con.Open();

                // 🔹 Borrar primero de la tabla puente
                using (var cmdDelEsp = new SqlCommand("DELETE FROM dbo.EspecialidadesVacantesTB WHERE IdVacante = @IdVacante", con))
                {
                    cmdDelEsp.Parameters.AddWithValue("@IdVacante", id);
                    cmdDelEsp.ExecuteNonQuery();
                }

                // 🔹 Lborrar la vacante
                using (var cmdDelVac = new SqlCommand("DELETE FROM dbo.VacantesPracticasTB WHERE IdVacante = @IdVacante", con))
                {
                    cmdDelVac.Parameters.AddWithValue("@IdVacante", id);
                    var rows = cmdDelVac.ExecuteNonQuery();

                    if (rows == 0)
                        return Json(new { ok = false, message = "No se encontró la vacante." });
                }

                return Json(new { ok = true, message = "Vacante eliminada correctamente" });
            }
        }

        // Validación común
        private List<string> ValidarCrearEditar(Vacante v)
{
    var errores = new List<string>();

    if (string.IsNullOrWhiteSpace(v.Nombre))
        errores.Add("El nombre es obligatorio.");
    if (v.IdEmpresa <= 0)
        errores.Add("La empresa es obligatoria.");
    if (string.IsNullOrWhiteSpace(v.Requerimientos))
        errores.Add("Los requisitos son obligatorios.");
    if (v.NumCupos < 1)
        errores.Add("Debe haber al menos 1 cupo.");
    if (!v.FechaMaxAplicacion.HasValue)
        errores.Add("La fecha límite de aplicación es obligatoria.");
    if (!v.FechaCierre.HasValue)
        errores.Add("La fecha de cierre es obligatoria.");
    if (v.FechaMaxAplicacion.HasValue && v.FechaCierre.HasValue &&
        v.FechaMaxAplicacion.Value.Date > v.FechaCierre.Value.Date)
        errores.Add("La fecha de aplicación no puede ser mayor a la fecha de cierre.");
    if (v.IdModalidad <= 0)
        errores.Add("La modalidad es obligatoria.");
    if (v.IdEspecialidad <= 0)
        errores.Add("La especialidad es obligatoria.");

    return errores;
}

// --------------------
// Métodos para llenar dropdowns
// --------------------
private List<SelectListItem> ObtenerEspecialidades()
{
    var lista = new List<SelectListItem>();

    using (var con = new SqlConnection(_cn))
    using (var cmd = new SqlCommand("SELECT IdEspecialidad, Nombre FROM EspecialidadesTB ORDER BY Nombre", con))
    {
        con.Open();
        using (var rd = cmd.ExecuteReader())
        {
            while (rd.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = rd["IdEspecialidad"].ToString(),
                    Text = rd["Nombre"].ToString()
                });
            }
        }
    }

    return lista;
}

private List<SelectListItem> ObtenerModalidades()
{
    var lista = new List<SelectListItem>();

    using (var con = new SqlConnection(_cn))
    using (var cmd = new SqlCommand("SELECT IdModalidad, Descripcion FROM ModalidadesTB ORDER BY Descripcion", con))
    {
        con.Open();
        using (var rd = cmd.ExecuteReader())
        {
            while (rd.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = rd["IdModalidad"].ToString(),
                    Text = rd["Descripcion"].ToString()
                });
            }
        }
    }

    return lista;
}
    }
}