using Microsoft.Ajax.Utilities;
using SIGEP.EF;
using SIGEP.Models;
using SIGEP.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace TuProyecto.Controllers
{
    [FiltroSesion]
    public class EmpresaController : Controller
    {
        private SIGEPEntities db = new SIGEPEntities();
        Utilitarios utilitarios = new Utilitarios();

        // ===========================
        // Helpers
        // ===========================
        private async Task<int> GetEstadoIdAsync(string descripcion, int fallback = 1)
        { 
            var estado = await db.EstadosTB
                                 .Where(e => e.Descripcion == descripcion)
                                 .Select(e => e.IdEstado)
                                 .FirstOrDefaultAsync();
            return estado == 0 ? fallback : estado;
        }

        private async Task<int?> ResolveDistritoIdAsync(string provincia, string canton, string distrito)
        {
            if (string.IsNullOrWhiteSpace(provincia) ||
                string.IsNullOrWhiteSpace(canton) ||
                string.IsNullOrWhiteSpace(distrito)) return null;

            var q =
                from p in db.ProvinciasTB
                join c in db.CantonesTB on p.IdProvincia equals c.IdProvincia
                join d in db.DistritosTB on c.IdCanton equals d.IdCanton
                where p.Nombre == provincia && c.Nombre == canton && d.Nombre == distrito
                select d.IdDistrito;

            return await q.FirstOrDefaultAsync();
        }

        // ===========================
        // Vista principal
        // ===========================
        [FiltroSesion]
        [FiltroCoordinador]
        [HttpGet]
        public ActionResult ListaEmpresas()
        {


          
            return View();
        }

        // ===========================
        // Listado para DataTable (AJAX)
        // ===========================
        
        [HttpGet]
        public async Task<JsonResult> GetEmpresas()
        {
            var activoId = await GetEstadoIdAsync("Activo", 1);

            var data = await (from emp in db.EmpresasTB
                              where emp.IdEstado == activoId
                              join dir in db.DireccionesTB on emp.IdDireccion equals dir.IdDireccion into d0
                              from dir in d0.DefaultIfEmpty()
                              join dis in db.DistritosTB on dir.IdDistrito equals dis.IdDistrito into d1
                              from dis in d1.DefaultIfEmpty()
                              join can in db.CantonesTB on dis.IdCanton equals can.IdCanton into d2
                              from can in d2.DefaultIfEmpty()
                              join pro in db.ProvinciasTB on can.IdProvincia equals pro.IdProvincia into d3
                              from pro in d3.DefaultIfEmpty()
                              select new EmpresaListVM
                              {
                                  IdEmpresa = emp.IdEmpresa,
                                  NombreEmpresa = emp.NombreEmpresa,
                                  AreasAfines = emp.AreasAfines,
                                  Ubicacion = (pro.Nombre ?? "") +
                                              (can.Nombre != null ? ", " + can.Nombre : "") +
                                              (dis.Nombre != null ? ", " + dis.Nombre : ""),
                                  HistorialVacantes = db.VacantesPracticasTB.Count(v => v.IdEmpresa == emp.IdEmpresa)
                              }).ToListAsync();

            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        // ===========================
        // Crear
        // ===========================
        [HttpPost]
        public async Task<JsonResult> CrearEmpresa(EmpresaCreateVM vm)
        {
            if (!ModelState.IsValid)
                return Json(new { ok = false, msg = "Datos incompletos." });

            try
            {
                var activoId = await GetEstadoIdAsync("Activo", 1);

                var idDireccion = utilitarios.ObtenerOCrearDireccion(db, vm.Provincia, vm.Canton, vm.Distrito, vm.Direccion, 0);

                // 2) Empresa
                var emp = new EmpresasTB
                {
                    NombreEmpresa = vm.NombreEmpresa,
                    NombreContacto = vm.NombreContacto,
                    IdDireccion = idDireccion,
                    AreasAfines = vm.Areas,
                    IdEstado = activoId 
                };
                db.EmpresasTB.Add(emp);
                await db.SaveChangesAsync();

                // 3) Email / Teléfono (opcionales)
                if (!string.IsNullOrWhiteSpace(vm.Email))
                {
                    db.EmailsTB.Add(new EmailsTB
                    {
                        IdEmpresa = emp.IdEmpresa,
                        Email = vm.Email
                    });
                }
                if (!string.IsNullOrWhiteSpace(vm.Telefono))
                {
                    db.TelefonosTB.Add(new TelefonosTB
                    {
                        IdEmpresa = emp.IdEmpresa,
                        Telefono = vm.Telefono
                    });
                }
                await db.SaveChangesAsync();

                return Json(new { ok = true, msg = "Empresa creada.", id = emp.IdEmpresa });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        // ===========================
        // Obtener una empresa por Id (para modal Editar)
        // ===========================
        [HttpGet]
        public async Task<JsonResult> GetEmpresa(int id)
        {
            var emp = await (from e in db.EmpresasTB
                             where e.IdEmpresa == id
                             join dir in db.DireccionesTB on e.IdDireccion equals dir.IdDireccion into d0
                             from dir in d0.DefaultIfEmpty()
                             join dis in db.DistritosTB on dir.IdDistrito equals dis.IdDistrito into d1
                             from dis in d1.DefaultIfEmpty()
                             join can in db.CantonesTB on dis.IdCanton equals can.IdCanton into d2
                             from can in d2.DefaultIfEmpty()
                             join pro in db.ProvinciasTB on can.IdProvincia equals pro.IdProvincia into d3
                             from pro in d3.DefaultIfEmpty()
                             select new EmpresaEditVM
                             {
                                 IdEmpresa = e.IdEmpresa,
                                 NombreEmpresa = e.NombreEmpresa,
                                 NombreContacto = e.NombreContacto,
                                 Email = db.EmailsTB.Where(x => x.IdEmpresa == e.IdEmpresa)
                                                    .Select(x => x.Email).FirstOrDefault(),
                                 Telefono = db.TelefonosTB.Where(x => x.IdEmpresa == e.IdEmpresa)
                                                          .Select(x => x.Telefono).FirstOrDefault(),
                                 Provincia = pro.Nombre,
                                 Canton = can.Nombre,
                                 Distrito = dis.Nombre,
                                 Direccion = dir.DireccionExacta,
                                 Areas = e.AreasAfines
                             }).FirstOrDefaultAsync();

            if (emp == null) return Json(new { ok = false, msg = "No encontrada" }, JsonRequestBehavior.AllowGet);
            return Json(new { ok = true, data = emp }, JsonRequestBehavior.AllowGet);
        }

        // ===========================
        // Editar
        // ===========================
        [HttpPost]
        public async Task<JsonResult> EditarEmpresa(EmpresaEditVM vm)
        {
            if (!ModelState.IsValid || vm.IdEmpresa <= 0)
                return Json(new { ok = false, msg = "Datos inválidos." });

            try
            {
                var emp = await db.EmpresasTB.FindAsync(vm.IdEmpresa);
             
                if (emp == null) return Json(new { ok = false, msg = "No existe." });

                var IdDireccion = 0;

                if (emp.IdDireccion != null)
                {
                     IdDireccion = (int)emp.IdDireccion;
                } else
                {
                    IdDireccion = 0;
                }

                    int idDireccion = utilitarios.ObtenerOCrearDireccion(
                           db,
                          vm.Provincia,
                          vm.Canton,
                         vm.Distrito,
                         vm.Direccion,
                         IdDireccion
                       );

                // Empresa
                emp.NombreEmpresa = vm.NombreEmpresa;
                emp.NombreContacto = vm.NombreContacto;
                emp.AreasAfines = vm.Areas;
                emp.IdDireccion = idDireccion;

                // Email (uno principal)
                var email = await db.EmailsTB.FirstOrDefaultAsync(x => x.IdEmpresa == emp.IdEmpresa);
                if (string.IsNullOrWhiteSpace(vm.Email))
                {
                    if (email != null) db.EmailsTB.Remove(email);
                }
                else
                {
                    if (email == null)
                        db.EmailsTB.Add(new EmailsTB { IdEmpresa = emp.IdEmpresa, Email = vm.Email });
                    else
                        email.Email = vm.Email;
                }

                // Teléfono (uno principal)
                var tel = await db.TelefonosTB.FirstOrDefaultAsync(x => x.IdEmpresa == emp.IdEmpresa);
                if (string.IsNullOrWhiteSpace(vm.Telefono))
                {
                    if (tel != null) db.TelefonosTB.Remove(tel);
                }
                else
                {
                    if (tel == null)
                        db.TelefonosTB.Add(new TelefonosTB { IdEmpresa = emp.IdEmpresa, Telefono = vm.Telefono });
                    else
                        tel.Telefono = vm.Telefono;
                }

                await db.SaveChangesAsync();
                return Json(new { ok = true, msg = "Cambios guardados." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        // ===========================
        // Eliminar (soft delete + cancelar vacantes de esa empresa)
        // ===========================
        [HttpPost]
        public async Task<JsonResult> EliminarEmpresa(int id)
        {
            try
            {
                var emp = await db.EmpresasTB.FindAsync(id);
                if (emp == null) return Json(new { ok = false, msg = "No existe." });

                var inactivoId = await GetEstadoIdAsync("Inactivo", 2);
                var canceladoId = await GetEstadoIdAsync("Cancelado", 3);

                emp.IdEstado = inactivoId;

                // Cancelar vacantes asociadas (si aplica)
                var vacs = await db.VacantesPracticasTB.Where(v => v.IdEmpresa == id).ToListAsync();
                foreach (var v in vacs) v.IdEstado = canceladoId;

                await db.SaveChangesAsync();
                return Json(new { ok = true, msg = "Empresa eliminada (inactiva) y vacantes canceladas." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, msg = ex.Message });
            }
        }

        // ===========================
        // Catálogos para combos
        // ===========================
        [HttpGet]
        public async Task<JsonResult> GetProvincias()
        {
            var list = await db.ProvinciasTB
                               .OrderBy(p => p.Nombre)
                               .Select(p => new { p.IdProvincia, p.Nombre })
                               .ToListAsync();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetCantones(int idProvincia)
        {
            var list = await db.CantonesTB
                               .Where(c => c.IdProvincia == idProvincia)
                               .OrderBy(c => c.Nombre)
                               .Select(c => new { c.IdCanton, c.Nombre })
                               .ToListAsync();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public async Task<JsonResult> GetDistritos(int idCanton)
        {
            var list = await db.DistritosTB
                               .Where(d => d.IdCanton == idCanton)
                               .OrderBy(d => d.Nombre)
                               .Select(d => new { d.IdDistrito, d.Nombre })
                               .ToListAsync();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }

    // ===========================
    // ViewModels
    // ===========================
    public class EmpresaListVM
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
        public string AreasAfines { get; set; }
        public string Ubicacion { get; set; }
        public int HistorialVacantes { get; set; }
    }

    public class EmpresaCreateVM
    {
        public string NombreEmpresa { get; set; }
        public string NombreContacto { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Provincia { get; set; }
        public string Canton { get; set; }
        public string Distrito { get; set; }
        public string Direccion { get; set; }
        public string Areas { get; set; }

        public int IdDireccion { get; set; }
    }

    public class EmpresaEditVM : EmpresaCreateVM
    {
        public int IdEmpresa { get; set; }
    }
}
