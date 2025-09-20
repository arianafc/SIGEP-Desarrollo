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

        // ==============================
        // VISTA PRINCIPAL
        // ==============================
        [HttpGet]
        public ActionResult VacantesEstudiantes()
        {
            ViewBag.Especialidades = ObtenerEspecialidades();
            ViewBag.Modalidades = ObtenerModalidades();
            return View();
        }

        // ==============================
        // LISTADO DE VACANTES
        // ==============================
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
                       ISNULL(sp.Nombre,'') AS EspecialidadNombre,
                       v.IdEstado,
                       ISNULL(es.Descripcion,'') AS EstadoNombre,
                       ISNULL(p.Postulados,0) AS EstudiantesPostulados
                FROM dbo.VacantesPracticasTB v
                LEFT JOIN dbo.EmpresasTB e  ON e.IdEmpresa = v.IdEmpresa
                LEFT JOIN dbo.EstadosTB es  ON es.IdEstado = v.IdEstado
                LEFT JOIN dbo.EspecialidadesVacantesTB ev ON ev.IdVacante = v.IdVacante
                LEFT JOIN dbo.EspecialidadesTB sp ON sp.IdEspecialidad = ev.IdEspecialidad
                LEFT JOIN dbo.ModalidadesTB m ON m.IdModalidad = v.IdModalidad
                LEFT JOIN (
                    SELECT IdVacante, COUNT(1) AS Postulados
                    FROM dbo.PracticaEstudianteTB
                    GROUP BY IdVacante
                ) p ON p.IdVacante = v.IdVacante
                WHERE (@estado = '' OR es.Descripcion = @estado)
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

        // ==============================
        // CREAR VACANTE
        // ==============================
        [HttpPost]
        public JsonResult Crear(Vacante v)
        {
            var errores = ValidarCrearEditar(v);
            if (errores.Count > 0)
                return Json(new { ok = false, message = string.Join("<br>", errores) });

            const int IdEstadoNoAsignada = 1; // "No asignada"

            using (var con = new SqlConnection(_cn))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        int idVacante;
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO dbo.VacantesPracticasTB
                                (Nombre, IdEmpresa, Requerimientos, FechaMaxAplicacion,
                                 NumCupos, FechaCierre, IdModalidad, Descripcion, IdEstado)
                            OUTPUT INSERTED.IdVacante
                            VALUES (@Nombre, @IdEmpresa, @Requerimientos, @FechaMaxAplicacion,
                                    @NumCupos, @FechaCierre, @IdModalidad, @Descripcion, @IdEstado);", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@Nombre", v.Nombre);
                            cmd.Parameters.AddWithValue("@IdEmpresa", v.IdEmpresa);
                            cmd.Parameters.AddWithValue("@Requerimientos", v.Requerimientos ?? "");
                            cmd.Parameters.AddWithValue("@FechaMaxAplicacion", (object)v.FechaMaxAplicacion ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NumCupos", v.NumCupos);
                            cmd.Parameters.AddWithValue("@FechaCierre", (object)v.FechaCierre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdModalidad", v.IdModalidad);
                            cmd.Parameters.AddWithValue("@Descripcion", v.Descripcion ?? "");
                            cmd.Parameters.AddWithValue("@IdEstado", IdEstadoNoAsignada);

                            idVacante = (int)cmd.ExecuteScalar();
                        }

                        if (v.IdEspecialidad > 0)
                        {
                            using (var cmdEsp = new SqlCommand(@"
                                INSERT INTO dbo.EspecialidadesVacantesTB (IdVacante, IdEspecialidad)
                                VALUES (@IdVacante, @IdEspecialidad);", con, tx))
                            {
                                cmdEsp.Parameters.AddWithValue("@IdVacante", idVacante);
                                cmdEsp.Parameters.AddWithValue("@IdEspecialidad", v.IdEspecialidad);
                                cmdEsp.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return Json(new { ok = true, message = "Vacante creada correctamente." });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { ok = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }

        // ==============================
        // DETALLE DE VACANTE
        // ==============================
        [HttpGet]
        public JsonResult Detalle(int id)
        {
            Vacante vacante = null;

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
               ISNULL(sp.Nombre,'') AS EspecialidadNombre,
               v.IdEstado,
               ISNULL(es.Descripcion,'') AS EstadoNombre
        FROM dbo.VacantesPracticasTB v
        LEFT JOIN dbo.EmpresasTB e  ON e.IdEmpresa = v.IdEmpresa
        LEFT JOIN dbo.EstadosTB es  ON es.IdEstado = v.IdEstado
        LEFT JOIN dbo.EspecialidadesVacantesTB ev ON ev.IdVacante = v.IdVacante
        LEFT JOIN dbo.EspecialidadesTB sp ON sp.IdEspecialidad = ev.IdEspecialidad
        LEFT JOIN dbo.ModalidadesTB m ON m.IdModalidad = v.IdModalidad
        WHERE v.IdVacante = @IdVacante;", con))
            {
                cmd.Parameters.AddWithValue("@IdVacante", id);

                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vacante = new Vacante
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
                            EstadoNombre = rd["EstadoNombre"] as string
                        };
                    }
                }
            }

            return Json(new { ok = vacante != null, data = vacante }, JsonRequestBehavior.AllowGet);
        }

        // ==============================
        // EDITAR VACANTE
        // ==============================
        [HttpPost]
        public JsonResult Editar(Vacante v)
        {
            var errores = ValidarCrearEditar(v, true);
            if (errores.Count > 0)
                return Json(new { ok = false, message = string.Join("<br>", errores) });

            using (var con = new SqlConnection(_cn))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(@"
                            UPDATE dbo.VacantesPracticasTB
                            SET Nombre = @Nombre,
                                IdEmpresa = @IdEmpresa,
                                Requerimientos = @Requerimientos,
                                FechaMaxAplicacion = @FechaMaxAplicacion,
                                NumCupos = @NumCupos,
                                FechaCierre = @FechaCierre,
                                IdModalidad = @IdModalidad,
                                Descripcion = @Descripcion
                            WHERE IdVacante = @IdVacante;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                            cmd.Parameters.AddWithValue("@Nombre", v.Nombre);
                            cmd.Parameters.AddWithValue("@IdEmpresa", v.IdEmpresa);
                            cmd.Parameters.AddWithValue("@Requerimientos", v.Requerimientos ?? "");
                            cmd.Parameters.AddWithValue("@FechaMaxAplicacion", (object)v.FechaMaxAplicacion ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NumCupos", v.NumCupos);
                            cmd.Parameters.AddWithValue("@FechaCierre", (object)v.FechaCierre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IdModalidad", v.IdModalidad);
                            cmd.Parameters.AddWithValue("@Descripcion", v.Descripcion ?? "");
                            cmd.ExecuteNonQuery();
                        }

                        // Actualizar tabla puente
                        using (var cmdDel = new SqlCommand("DELETE FROM dbo.EspecialidadesVacantesTB WHERE IdVacante = @IdVacante;", con, tx))
                        {
                            cmdDel.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                            cmdDel.ExecuteNonQuery();
                        }

                        if (v.IdEspecialidad > 0)
                        {
                            using (var cmdEsp = new SqlCommand(@"
                                INSERT INTO dbo.EspecialidadesVacantesTB (IdVacante, IdEspecialidad)
                                VALUES (@IdVacante, @IdEspecialidad);", con, tx))
                            {
                                cmdEsp.Parameters.AddWithValue("@IdVacante", v.IdVacante);
                                cmdEsp.Parameters.AddWithValue("@IdEspecialidad", v.IdEspecialidad);
                                cmdEsp.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                        return Json(new { ok = true, message = "Vacante actualizada correctamente." });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { ok = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }

        // ==============================
        // ELIMINAR VACANTE
        // ==============================
        [HttpPost]
        public JsonResult Eliminar(int id)
        {
            using (var con = new SqlConnection(_cn))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmdDelEsp = new SqlCommand("DELETE FROM dbo.EspecialidadesVacantesTB WHERE IdVacante = @IdVacante;", con, tx))
                        {
                            cmdDelEsp.Parameters.AddWithValue("@IdVacante", id);
                            cmdDelEsp.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand("DELETE FROM dbo.VacantesPracticasTB WHERE IdVacante = @IdVacante;", con, tx))
                        {
                            cmd.Parameters.AddWithValue("@IdVacante", id);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return Json(new { ok = true, message = "Vacante eliminada correctamente." });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        return Json(new { ok = false, message = "Error: " + ex.Message });
                    }
                }
            }
        }

        //obtener postulaciones de una vacante
        [HttpGet]
        public JsonResult ObtenerPostulaciones(int idVacante)
        {
            var usuarios = new List<object>();

            using (SqlConnection conn = new SqlConnection(_cn))
            {
                conn.Open();

                string query = @"
            SELECT u.IdUsuario, u.Cedula, u.Nombre, u.Apellido1, u.Apellido2
            FROM PracticaEstudianteTB p
            INNER JOIN UsuariosTB u ON p.IdUsuario = u.IdUsuario
            WHERE p.IdVacante = @IdVacante
            ORDER BY u.Nombre ASC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@IdVacante", idVacante);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            usuarios.Add(new
                            {
                                IdUsuario = (int)reader["IdUsuario"],
                                Cedula = reader["Cedula"].ToString(),
                                NombreCompleto = $"{reader["Nombre"]} {reader["Apellido1"]} {reader["Apellido2"]}"
                            });
                        }
                    }
                }
            }

            return Json(new { ok = true, data = usuarios }, JsonRequestBehavior.AllowGet);
        }


        // ==============================
        // VALIDACIÓN
        // ==============================
        private List<string> ValidarCrearEditar(Vacante v, bool esEditar = false)
        {
            var errores = new List<string>();
            if (esEditar && v.IdVacante <= 0)
                errores.Add("Id de vacante inválido.");
            if (string.IsNullOrWhiteSpace(v.Nombre))
                errores.Add("El nombre es requerido.");
            if (v.IdEmpresa <= 0)
                errores.Add("Debe seleccionar una empresa.");
            if (v.NumCupos <= 0)
                errores.Add("Debe ingresar un número válido de cupos.");
            if (v.FechaMaxAplicacion.HasValue && v.FechaCierre.HasValue &&
                v.FechaMaxAplicacion.Value.Date > v.FechaCierre.Value.Date)
                errores.Add("La fecha de aplicación no puede ser posterior a la fecha de cierre.");
            return errores;
        }

        // ==============================
        // LISTAS PARA DROPDOWNS
        // ==============================
        private List<SelectListItem> ObtenerEspecialidades()
        {
            var list = new List<SelectListItem>();
            using (var con = new SqlConnection(_cn))
            using (var cmd = new SqlCommand("SELECT IdEspecialidad, Nombre FROM dbo.EspecialidadesTB ORDER BY Nombre;", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = rd["IdEspecialidad"].ToString(),
                            Text = rd["Nombre"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        private List<SelectListItem> ObtenerModalidades()
        {
            var list = new List<SelectListItem>();
            using (var con = new SqlConnection(_cn))
            using (var cmd = new SqlCommand("SELECT IdModalidad, Descripcion FROM dbo.ModalidadesTB ORDER BY Descripcion;", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new SelectListItem
                        {
                            Value = rd["IdModalidad"].ToString(),
                            Text = rd["Descripcion"].ToString()
                        });
                    }
                }
            }
            return list;
        }
    }
}