using System.Web;
using System.Web.Mvc;

namespace QuanLyQuanNuoc_65130449
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
