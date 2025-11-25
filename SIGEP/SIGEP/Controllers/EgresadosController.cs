using SIGEP.EF;
using SIGEP.Models;
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
                            Correo = db.EmailsTB.Where(e => e.IdUsuario == u.IdUsuario).Select(e => e.Email).FirstOrDefault(),
                            Telefono = db.TelefonosTB.Where(t => t.IdUsuario == u.IdUsuario).Select(t => t.Telefono).FirstOrDefault()
                        };

            // 🔹 Filtrar por especialidad
            if (idEspecialidad > 0)
            {
                query = query.Where(x => x.IdEspecialidad == idEspecialidad);
            }

            // 🔹 Filtrar por año de egreso
            if (anio > 0)
            {
                query = query.Where(x => x.FechaEgreso.HasValue && x.FechaEgreso.Value.Year == anio);
            }

            // 🔹 Proyectar resultado final
            var lista = query
                .AsEnumerable() // ejecutar antes de manipular datos calculados
                .Select(x => new
                {
                    NombreCompleto = $"{x.Nombre} {x.Apellido1} {x.Apellido2}",
                    Generacion = x.FechaEgreso.HasValue ? x.FechaEgreso.Value.Year.ToString() : "N/A",
                    x.Especialidad,
                    x.Correo,
                    x.Telefono
                })
                .ToList();

            return Json(lista, JsonRequestBehavior.AllowGet);
        }


        #region Gestion de bolsa de empleos



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


                    int idDireccion = ObtenerOCrearDireccion(
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
                    int idDireccion = ObtenerOCrearDireccion(
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



        private int ObtenerOCrearDireccion(
     SIGEPEntities db,
     string nombreProvincia,
     string nombreCanton,
     string nombreDistrito,
     string direccionExacta,
     int idDireccion // 0 o negativo si es NUEVA dirección
 )
        {
            if (string.IsNullOrWhiteSpace(nombreProvincia))
                throw new ArgumentException("La provincia es requerida.");

            if (string.IsNullOrWhiteSpace(nombreCanton))
                throw new ArgumentException("El cantón es requerido.");

            if (string.IsNullOrWhiteSpace(nombreDistrito))
                throw new ArgumentException("El distrito es requerido.");

            if (string.IsNullOrWhiteSpace(direccionExacta))
                throw new ArgumentException("La dirección exacta es requerida.");

            // 1. Provincia
            var provincia = db.ProvinciasTB
                              .FirstOrDefault(p => p.Nombre == nombreProvincia);

            if (provincia == null)
            {
                provincia = new ProvinciasTB
                {
                    Nombre = nombreProvincia
                };
                db.ProvinciasTB.Add(provincia);
                db.SaveChanges();
            }

            // 2. Cantón
            var canton = db.CantonesTB
                           .FirstOrDefault(c => c.Nombre == nombreCanton
                                             && c.IdProvincia == provincia.IdProvincia);

            if (canton == null)
            {
                canton = new CantonesTB
                {
                    Nombre = nombreCanton,
                    IdProvincia = provincia.IdProvincia
                };
                db.CantonesTB.Add(canton);
                db.SaveChanges();
            }

            // 3. Distrito
            var distrito = db.DistritosTB
                             .FirstOrDefault(d => d.Nombre == nombreDistrito
                                               && d.IdCanton == canton.IdCanton);

            if (distrito == null)
            {
                distrito = new DistritosTB
                {
                    Nombre = nombreDistrito,
                    IdCanton = canton.IdCanton
                };
                db.DistritosTB.Add(distrito);
                db.SaveChanges();
            }

            DireccionesTB direccion = null;

          
            if (idDireccion > 0)
            {
                direccion = db.DireccionesTB
                              .FirstOrDefault(di => di.IdDireccion == idDireccion);

                if (direccion != null)
                {
                    direccion.DireccionExacta = direccionExacta;
                    direccion.IdDistrito = distrito.IdDistrito;
                    
                    db.SaveChanges();
                    return direccion.IdDireccion;
                }
            }

           
            direccion = new DireccionesTB
            {
                IdDistrito = distrito.IdDistrito,
                DireccionExacta = direccionExacta,
                IdEstado = 1
            };

            db.DireccionesTB.Add(direccion);
            db.SaveChanges();

            return direccion.IdDireccion;
        }


    }
    #endregion


}
