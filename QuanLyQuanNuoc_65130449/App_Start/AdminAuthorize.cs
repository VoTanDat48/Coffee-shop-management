using System.Web;
using System.Web.Mvc;

namespace QuanLyQuanNuoc_65130449.Models // Hoặc namespace dự án của bạn
{
    public class AdminAuthorize : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // 1. Lấy thông tin session
            var session = filterContext.HttpContext.Session;

            // 2. Kiểm tra: Nếu chưa đăng nhập HOẶC không phải là "Admin"
            if (session["UserRole"] == null || session["UserRole"].ToString() != "Admin")
            {
                // Cho khách hàng "out" ra trang báo lỗi hoặc trang đăng nhập
                // Ở đây mình chuyển về trang Login
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary {
                        { "controller", "AccountController_65130449" },
                        { "action", "Login" }
                    });
            }
            base.OnActionExecuting(filterContext);
        }
    }
}