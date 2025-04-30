using APPWeb20DeFeb.Filters;
using System.Web;
using System.Web.Mvc;

namespace APPWeb20DeFeb
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
           // filters.Add(new VerificarSession());
        }
    }
}
