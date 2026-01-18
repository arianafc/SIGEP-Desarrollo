using SIGEP.EF;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SIGEP.Models
{
    public class FiltroSesion : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;

            var idRolObj = contexto.Session["IdRol"];
            var idRol = idRolObj != null ? idRolObj.ToString() : null;

         
            if (contexto.Session.Count == 0 || string.IsNullOrEmpty(idRol))
            {
                filterContext.Controller.TempData["SwalError"] = "Debes iniciar sesión para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Login");
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class ValidarUsuarioActivoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = HttpContext.Current.Session;

            if (session == null || session["IdUsuario"] == null)
            {
                RedirigirLogin(filterContext);
                return;
            }

            int idUsuario = (int)session["IdUsuario"];

            using (var db = new SIGEPEntities())
            {
                var usuario = db.UsuariosTB.FirstOrDefault(u => u.IdUsuario == idUsuario);

                if (usuario == null || usuario.IdEstado == 2)
                {
                    session.Clear();
                    session.Abandon();

                    RedirigirLogin(filterContext);
                }
            }

            base.OnActionExecuting(filterContext);
        }

        private void RedirigirLogin(ActionExecutingContext context)
        {



            context.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary
                {
                { "controller", "Home" },
                { "action", "Login" }
                });
        }

    }
        public class FiltroEstudiante : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;
            var rol = contexto.Session["IdRol"] != null ? contexto.Session["IdRol"].ToString() : null;

            if (string.IsNullOrEmpty(rol) || rol != "1")
            {
                filterContext.Controller.TempData["SwalError"] = "No tienes permiso para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class FiltroUsuarioAdmin : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;
            var rol = contexto.Session["IdRol"] != null ? contexto.Session["IdRol"].ToString() : null;

            if (string.IsNullOrEmpty(rol) || (rol != "2" && rol != "3"))
            {
                filterContext.Controller.TempData["SwalError"] = "No tienes permiso para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }

    public class FiltroProfesor : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;
            var rol = contexto.Session["IdRol"] != null ? contexto.Session["IdRol"].ToString() : null;

            if (string.IsNullOrEmpty(rol) || rol != "3")
            {
                filterContext.Controller.TempData["SwalError"] = "No tienes permiso para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }


    public class FiltroCoordinador : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;
            var rol = contexto.Session["IdRol"] != null ? contexto.Session["IdRol"].ToString() : null;

            if (string.IsNullOrEmpty(rol) || rol != "2")
            {
                filterContext.Controller.TempData["SwalError"] = "No tienes permiso para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }


    public class FiltroEgresado : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var contexto = filterContext.HttpContext;
            var rol = contexto.Session["IdRol"] != null ? contexto.Session["IdRol"].ToString() : null;

            if (string.IsNullOrEmpty(rol) || rol != "4")
            {
                filterContext.Controller.TempData["SwalError"] = "No tienes permiso para acceder a esta página.";
                filterContext.Result = new RedirectResult("~/Home/Index");
            }

            base.OnActionExecuting(filterContext);
        }
    }

}