using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class AdminController_65130449 : Controller
    {
        // GET: AdminController_65130449
        public ActionResult Index()
        {
            return View();
        }
        
        // Sản phẩm
        public ActionResult DS_SP()
        {
            return View();
        }

        //Nhân viên
        public ActionResult DS_NV()
        {
            return View();
        }

        //Thống kê
        public ActionResult DoanhThu()
        {
            return View();
        }
    }
}