using Microsoft.Ajax.Utilities;
using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SIGEP.Controllers
{
    [FiltroSesion]
    public class EgresadosController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();


        [FiltroSesion]
        [ValidarUsuarioActivo]
        [FiltroCoordinador]
        [HttpGet]
        public ActionResult ListaEgresados()
        {
            // Obtener el rol de sesión
            var idRol = Session["IdRol"] != null ? Convert.ToInt32(Session["IdRol"]) : 0;

            // Validar: solo Coordinador (rol 2) puede acceder
            if (idRol != 2)
            {
                return RedirectToAction("Login", "Home");
            }

            ViewBag.Especialidades = db.EspecialidadesTB
                .OrderBy(e => e.Nombre)
                .Select(e => new SelectListItem
                {
                    Value = e.IdEspecialidad.ToString(),
                    Text = e.Nombre
                }).ToList();

            ViewBag.Anios = db.UsuariosTB
                .Where(u => u.IdRol == 4 && u.FechaEgreso.HasValue)
                .Select(u => u.FechaEgreso.Value.Year)
                .Distinct()
                .OrderByDescending(a => a)
                .Select(a => new SelectListItem
                {
                    Value = a.ToString(),
                    Text = a.ToString()
                }).ToList();

            return View();
        }

        // Agrega estas clases dentro del controlador
        public class FormacionDTO
        {
            public string Carrera { get; set; }
            public string Titulo { get; set; }
            public int? AnnoGraduacion { get; set; }
        }

        public class LaboralDTO
        {
            public string EmpresaActual { get; set; }
            public string PuestoActual { get; set; }
        }

        [HttpGet]
        public JsonResult ObtenerEgresados(int idEspecialidad = 0, int anio = 0)
        {
            var query = from u in db.UsuariosTB
                        join ue in db.UsuarioEspecialidadTB on u.IdUsuario equals ue.IdUsuario into jue
                        from ue in jue.DefaultIfEmpty()
                        join esp in db.EspecialidadesTB on ue.IdEspecialidad equals esp.IdEspecialidad into jesp
                        from esp in jesp.DefaultIfEmpty()
                        where u.IdRol == 4
                        select new
                        {
                            u.IdUsuario,
                            u.Nombre,
                            u.Apellido1,
                            u.Apellido2,
                            u.FechaEgreso,
                            Especialidad = esp != null ? esp.Nombre : "Sin especialidad",
                            IdEspecialidad = ue != null ? ue.IdEspecialidad : 0,
                            Correo = db.EmailsTB
                                .Where(e => e.IdUsuario == u.IdUsuario)
                                .Select(e => e.Email)
                                .FirstOrDefault(),
                            Telefono = db.TelefonosTB
                                .Where(t => t.IdUsuario == u.IdUsuario)
                                .Select(t => t.Telefono)
                                .FirstOrDefault()
                        };

            if (idEspecialidad > 0)
                query = query.Where(x => x.IdEspecialidad == idEspecialidad);

            if (anio > 0)
                query = query.Where(x => x.FechaEgreso.HasValue && x.FechaEgreso.Value.Year == anio);

            var usuarios = query.AsEnumerable()
                .Select(x => new
                {
                    x.IdUsuario,
                    NombreCompleto = $"{x.Nombre} {x.Apellido1} {x.Apellido2}".Trim(),
                    Generacion = x.FechaEgreso.HasValue ? x.FechaEgreso.Value.Year.ToString() : "N/A",
                    x.Especialidad,
                    Correo = x.Correo ?? "—",
                    Telefono = x.Telefono ?? "—"
                }).ToList();

            var ids = usuarios.Select(u => u.IdUsuario).ToList();

            // ✅ Usar clases DTO concretas para que Json() serialice correctamente
            var formacionMap = db.FormacionAcademicaTB
                .Where(f => ids.Contains(f.IdUsuario))
                .ToList() // materializar primero
                .GroupBy(f => f.IdUsuario)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => new FormacionDTO
                    {
                        Carrera = f.Carrera,
                        Titulo = f.Titulo,
                        AnnoGraduacion = f.AnnoGraduacion
                    }).ToList()
                );

            var laboralMap = db.InformacionLaboralTB
                .Where(l => ids.Contains(l.IdUsuario))
                .ToList() // materializar primero
                .GroupBy(l => l.IdUsuario)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(l => new LaboralDTO
                    {
                        EmpresaActual = l.EmpresaActual,
                        PuestoActual = l.PuestoActual
                    }).ToList()
                );

            // Debug temporal — quítalo después de confirmar que funciona
            System.Diagnostics.Debug.WriteLine($"IDs buscados: {string.Join(",", ids)}");
            System.Diagnostics.Debug.WriteLine($"Formaciones encontradas: {formacionMap.Count}");
            System.Diagnostics.Debug.WriteLine($"Laborales encontradas: {laboralMap.Count}");

            var lista = usuarios.Select(u => new
            {
                u.NombreCompleto,
                u.Generacion,
                u.Especialidad,
                u.Correo,
                u.Telefono,
                Formacion = formacionMap.ContainsKey(u.IdUsuario)
                    ? formacionMap[u.IdUsuario]
                    : new List<FormacionDTO>(),
                Laboral = laboralMap.ContainsKey(u.IdUsuario)
                    ? laboralMap[u.IdUsuario]
                    : new List<LaboralDTO>()
            }).ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }


        #region Gestion de bolsa de empleos


        [FiltroSesion]
        [ValidarUsuarioActivo]
        [FiltroCoordinador]
        [HttpGet]

        public ActionResult BolsaEmpleo()
        {

            var BolsaEmpleo = new BolsaEmpleoVM();
            ViewBag.Modalidades = db.ModalidadesTB
                .OrderBy(m => m.Descripcion)
                .Select(m => new SelectListItem
                {
                    Value = m.IdModalidad.ToString(),
                    Text = m.Descripcion.ToString(),
                }).ToList();

            ViewBag.Estados = db.EstadosTB
    .Where(e => e.Descripcion == "Activo" || e.Descripcion == "Inactivo")
    .OrderBy(e => e.Descripcion)
    .Select(e => new SelectListItem
    {
        Value = e.IdEstado.ToString(),
        Text = e.Descripcion
    })
    .ToList();


            using (var db = new SIGEPEntities())
            {
                var ofertas = db.ObtenerBolsaEmpleoSP().ToList();
                BolsaEmpleo.ListaEmpleos = ofertas.Select(o => new BolsaEmpleoModel
                {
                    IdEmpleo = o.IdEmpleo,
                    Empresa = o.Empresa,
                    NombrePuesto = o.NombrePuesto,
                    Descripcion = o.Descripcion,
                    Requisitos = o.Requisitos,
                    FechaPublicacion = o.FechaPublicacion,
                    FechaLimite = o.FechaLimite,
                    IdEstado = o.IdEstado,
                    AreaAfin = o.AreaAfin,
                    Canton = o.Canton,
                    Provincia = o.Provincia,
                    Distrito = o.Distrito,
                    DireccionExacta = o.DireccionExacta,
                    IdDireccion = (int)o.IdDireccion,
                    IdModalidad = (int)o.IdModalidad,
                    Modalidad = o.Modalidad
                }).ToList();




                return View(BolsaEmpleo);
            }

        }



        [HttpPost]

        public ActionResult CrearEmpleo(BolsaEmpleoModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    ok = false,
                    msg = "Datos incompletos o inválidos."
                }, JsonRequestBehavior.AllowGet);
            }

            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {

                    int idEstadoPublicado = 1;


                    int idDireccion = utilitarios.ObtenerOCrearDireccion(
                        db,
                        model.Provincia,
                        model.Canton,
                        model.Distrito,
                        model.DireccionExacta,
                        0
                    );


                    var empleo = new BolsaEmpleoTB
                    {
                        Empresa = model.Empresa,
                        Descripcion = model.Descripcion,
                        Requisitos = model.Requisitos,
                        FechaPublicacion = DateTime.Now,
                        FechaLimite = model.FechaLimite,
                        IdEstado = idEstadoPublicado,
                        AreaAfin = model.AreaAfin,
                        IdDireccion = idDireccion,
                        NombrePuesto = model.NombrePuesto,
                        IdModalidad = model.IdModalidad
                    };

                    db.BolsaEmpleoTB.Add(empleo);
                    db.SaveChanges();

                    tx.Commit();

                    return Json(new
                    {
                        ok = true,
                        msg = "El empleo se ha registrado correctamente."
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    tx.Rollback();

                    return Json(new
                    {
                        ok = false,
                        msg = "Ocurrió un error al guardar la información."
                    }, JsonRequestBehavior.AllowGet);
                }
            }
        }


        [HttpPost]
        public ActionResult EditarEmpleo(BolsaEmpleoModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    ok = false,
                    msg = "Datos incompletos o inválidos."
                }, JsonRequestBehavior.AllowGet);
            }
            using (var db = new SIGEPEntities())
            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    var empleo = db.BolsaEmpleoTB
                                   .FirstOrDefault(e => e.IdEmpleo == model.IdEmpleo);
                    if (empleo == null)
                    {
                        return Json(new
                        {
                            ok = false,
                            msg = "El empleo no existe."
                        }, JsonRequestBehavior.AllowGet);
                    }
                    int idDireccion = utilitarios.ObtenerOCrearDireccion(
                        db,
                        model.Provincia,
                        model.Canton,
                        model.Distrito,
                        model.DireccionExacta,
                        model.IdDireccion
                    );
                    empleo.Empresa = model.Empresa;
                    empleo.Descripcion = model.Descripcion;
                    empleo.Requisitos = model.Requisitos;
                    empleo.FechaLimite = model.FechaLimite;
                    empleo.AreaAfin = model.AreaAfin;
                    empleo.IdDireccion = idDireccion;
                    empleo.NombrePuesto = model.NombrePuesto;
                    empleo.IdModalidad = model.IdModalidad;
                    db.SaveChanges();
                    tx.Commit();
                    return Json(new
                    {
                        ok = true,
                        msg = "El empleo se ha actualizado correctamente."
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Json(new
                    {
                        ok = false,
                        msg = "Ocurrió un error al guardar la información."
                    }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpPost]
        public ActionResult CambiarEstado(int IdEmpleo)
        {

               var empleo = db.BolsaEmpleoTB
                                   .FirstOrDefault(e => e.IdEmpleo == IdEmpleo);

                if(empleo.IdEstado == 1)
            {
                empleo.IdEstado = 2;
                db.SaveChanges();

                return Json(new
                {
                    ok = true,
                    msg = "Empleo desactivado correctamente."
                }, JsonRequestBehavior.AllowGet);

            }
            else
            {
                empleo.IdEstado = 1;
                db.SaveChanges();

                return Json(new
                {
                    ok = true,
                    msg = "Empleo activado correctamente."
                }, JsonRequestBehavior.AllowGet);

            }



        }



    }
    #endregion


}
