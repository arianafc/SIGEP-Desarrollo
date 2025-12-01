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