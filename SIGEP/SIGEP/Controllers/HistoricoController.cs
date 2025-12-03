using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using SIGEP.EF;
using SIGEP.Models;
using System.Data.SqlClient;

namespace SIGEP.Controllers
{
    public class HistoricoController : Controller
    {
        // ============================================
        // VALIDACIÓN GLOBAL PARA TODAS LAS ACCIONES
        // ============================================
        private ActionResult ValidarAcceso()
        {
            // No hay sesión activa
            if (Session["IdRol"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            int idRol = Convert.ToInt32(Session["IdRol"]);

            // Solo rol 2 permitido
            if (idRol != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            return null; // acceso permitido
        }

        // ============================================
        // MODELOS INTERNOS
        // ============================================
        public class HistoricoItem
        {
            public int IdPractica { get; set; }
            public int IdVacante { get; set; }
            public int IdUsuario { get; set; }
            public string Nombre { get; set; }
            public string Apellido1 { get; set; }
            public string Apellido2 { get; set; }
            public DateTime FechaAplicacion { get; set; }
            public int IdEstado { get; set; }
            public string NombreVacante { get; set; }
            public string Requerimientos { get; set; }
            public DateTime? FechaMaxAplicacion { get; set; }
            public int? NumCupos { get; set; }
            public DateTime? FechaCierre { get; set; }
            public string Descripcion { get; set; }
            public string Tipo { get; set; }
            public string Modalidad { get; set; }
            public int IdEspecialidad { get; set; }
            public string Especialidad { get; set; }

            public string NombreCompleto
            {
                get { return (Nombre + " " + Apellido1 + " " + Apellido2).Trim(); }
            }
        }

        public class ComentarioItem
        {
            public string Autor { get; set; }
            public DateTime Fecha { get; set; }
            public string Comentario { get; set; }
        }

        public class DetallePracticaItem
        {
            public int IdVacante { get; set; }
            public string Nombre { get; set; }
            public string EmpresaNombre { get; set; }
            public string Requerimientos { get; set; }
            public DateTime? FechaMaxAplicacion { get; set; }
            public string ModalidadNombre { get; set; }

            public int IdUsuario { get; set; }
            public string EstudianteNombre { get; set; }
            public string EstudianteCedula { get; set; }
            public int EstudianteEdad { get; set; }
            public string EstudianteEspecialidad { get; set; }
            public string EstudianteCorreo { get; set; }

            public string ContactoEmpresaNombre { get; set; }
            public string ContactoEmpresaEmail { get; set; }
            public string ContactoEmpresaTelefono { get; set; }

            public int IdPractica { get; set; }
            public DateTime FechaAplicacion { get; set; }
            public string EstadoPractica { get; set; }

            public decimal? Nota1 { get; set; }
            public decimal? Nota2 { get; set; }
            public decimal? NotaFinal { get; set; }
        }

        // ============================================
        // OBTENER HISTÓRICO
        // ============================================
        private List<HistoricoItem> ObtenerHistorico()
        {
            using (var db = new SIGEPEntities())
            {
                return db.Database.SqlQuery<HistoricoItem>("HistoricoPracticasSP").ToList();
            }
        }

        // ============================================
        // VISTA PRINCIPAL
        // ============================================
        public ActionResult HistoricoPracticas()
        {
            var acceso = ValidarAcceso();
            if (acceso != null) return acceso;

            var data = ObtenerHistorico();
            return View("~/Views/Historico/HistoricoPracticas.cshtml", data);
        }

        // ============================================
        // EXPORTAR EXCEL
        // ============================================
        public ActionResult ExportarExcel()
        {
            var acceso = ValidarAcceso();
            if (acceso != null) return acceso;

            var data = ObtenerHistorico();
            var sb = new StringBuilder();

            sb.Append("<table border='1' style='border-collapse:collapse;'>");
            sb.Append("<tr style='background-color:#2D594D;color:#ffffff;font-weight:bold;'>");
            sb.Append("<th>Id Práctica</th>");
            sb.Append("<th>Estudiante</th>");
            sb.Append("<th>Especialidad</th>");
            sb.Append("<th>Vacante</th>");
            sb.Append("<th>Modalidad</th>");
            sb.Append("<th>Tipo</th>");
            sb.Append("<th>Fecha Aplicación</th>");
            sb.Append("<th>Fecha Máx.</th>");
            sb.Append("<th>Fecha Cierre</th>");
            sb.Append("<th>Cupos</th>");
            sb.Append("<th>Estado</th>");
            sb.Append("</tr>");

            foreach (var x in data)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td>{0}</td>", x.IdPractica);
                sb.AppendFormat("<td>{0}</td>", x.NombreCompleto);
                sb.AppendFormat("<td>{0}</td>", x.Especialidad);
                sb.AppendFormat("<td>{0}</td>", x.NombreVacante);
                sb.AppendFormat("<td>{0}</td>", x.Modalidad);
                sb.AppendFormat("<td>{0}</td>", x.Tipo);
                sb.AppendFormat("<td>{0}</td>", x.FechaAplicacion.ToString("dd/MM/yyyy"));
                sb.AppendFormat("<td>{0}</td>", x.FechaMaxAplicacion.HasValue ? x.FechaMaxAplicacion.Value.ToString("dd/MM/yyyy") : "");
                sb.AppendFormat("<td>{0}</td>", x.FechaCierre.HasValue ? x.FechaCierre.Value.ToString("dd/MM/yyyy") : "");
                sb.AppendFormat("<td>{0}</td>", x.NumCupos.HasValue ? x.NumCupos.Value.ToString() : "");
                sb.AppendFormat("<td>{0}</td>", x.IdEstado);
                sb.Append("</tr>");
            }

            sb.Append("</table>");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var nombre = "HistoricoPracticas_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".xls";

            return File(bytes, "application/vnd.ms-excel", nombre);
        }

        // ============================================
        // COMENTARIOS
        // ============================================
        public ActionResult ObtenerComentarios(int idUsuario)
        {
            var acceso = ValidarAcceso();
            if (acceso != null) return acceso;

            using (var db = new SIGEPEntities())
            {
                var p = new SqlParameter("@IdUsuario", idUsuario);

                var lista = db.Database.SqlQuery<ComentarioItem>(
                    "ObtenerComentariosEstudianteSP @IdUsuario", p
                ).ToList();

                var result = lista.Select(c => new
                {
                    autor = c.Autor,
                    fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"),
                    comentario = c.Comentario
                });

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        // ============================================
        // DETALLE DE EMPRESA / VACANTE
        // ============================================
        public ActionResult ObtenerDetallePractica(int idVacante, int idUsuario)
        {
            var acceso = ValidarAcceso();
            if (acceso != null) return acceso;

            using (var db = new SIGEPEntities())
            {
                var pVac = new SqlParameter("@IdVacante", idVacante);
                var pUsr = new SqlParameter("@IdUsuario", idUsuario);

                var detalle = db.Database.SqlQuery<DetallePracticaItem>(
                    "ObtenerVisualizacionPracticaSP @IdVacante, @IdUsuario",
                    pVac,
                    pUsr
                ).FirstOrDefault();

                if (detalle == null)
                {
                    return Json(null, JsonRequestBehavior.AllowGet);
                }

                var result = new
                {
                    vacante = detalle.Nombre,
                    empresa = detalle.EmpresaNombre,
                    requerimientos = detalle.Requerimientos,
                    fechaMaxAplicacion = detalle.FechaMaxAplicacion?.ToString("dd/MM/yyyy"),
                    modalidad = detalle.ModalidadNombre,
                    estudiante = detalle.EstudianteNombre,
                    cedulaEstudiante = detalle.EstudianteCedula,
                    edadEstudiante = detalle.EstudianteEdad,
                    especialidadEstudiante = detalle.EstudianteEspecialidad,
                    correoEstudiante = detalle.EstudianteCorreo,
                    contactoNombre = detalle.ContactoEmpresaNombre,
                    contactoCorreo = detalle.ContactoEmpresaEmail,
                    contactoTelefono = detalle.ContactoEmpresaTelefono,
                    estadoPractica = detalle.EstadoPractica,
                    nota1 = detalle.Nota1,
                    nota2 = detalle.Nota2,
                    notaFinal = detalle.NotaFinal
                };

                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
