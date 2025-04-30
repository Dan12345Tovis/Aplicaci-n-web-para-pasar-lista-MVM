using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace APPWeb20DeFeb.Filters
{                                  //se le agrega el filtro
    public class VerificarSession: ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Verificar si la sesión "Usuario" está activa
            if (HttpContext.Current.Session["Usuario"] == null)
            {
                // Redirigir a la página de login si la sesión no existe
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Cuenta" },
                        { "action", "Login" }
                    }
                );
            }
        }
    }
}