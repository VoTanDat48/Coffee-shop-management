using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using QuanLyQuanNuoc_65130449.Models; // Namespace chứa NhanVien, KhachHang

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class AccountController_65130449Controller : Controller
    {
        // DbContext sinh ra từ Entity Framework (tên hiện tại: QuanLyQuanNuoc_65130449Entities1)
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1 db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1();

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string TenDangNhap, string MatKhau, string returnUrl)
        {
            if (string.IsNullOrEmpty(TenDangNhap) || string.IsNullOrEmpty(MatKhau))
            {
                ModelState.AddModelError("", "Tên đăng nhập và mật khẩu không được để trống.");
                return View();
            }

            // Kiểm tra Nhân viên
            var nhanVien = db.NhanViens
                             .FirstOrDefault(nv => nv.TenDangNhap == TenDangNhap && nv.MatKhau == MatKhau);

            if (nhanVien != null)
            {
                FormsAuthentication.SetAuthCookie(nhanVien.TenDangNhap, false);

                // Phân quyền theo Vai trò
                switch (nhanVien.VaiTro)
                {
                    case 0: // Quản lý/Admin
                        return RedirectToAction("Dashboard", "Admin"); // Trang quản lý tổng
                    case 1: // Nhân viên Duyệt
                        return RedirectToAction("Processing", "Employee"); // Trang duyệt đơn
                    case 2: // Nhân viên Giao hàng
                        return RedirectToAction("Delivery", "Employee"); // Trang giao hàng
                    
                }
            }


            // Kiểm tra Khách hàng
            var khachHang = db.KhachHangs
                              .FirstOrDefault(kh => kh.TenDangNhap == TenDangNhap && kh.MatKhau == MatKhau);

            if (khachHang != null)
            {
                FormsAuthentication.SetAuthCookie(khachHang.HoTen, false);

                // ✅ Lưu thông tin vào Session để layout hiển thị đúng
                Session["UserRole"] = "khachhang"; // Phân quyền
                Session["MaKH"] = khachHang.MaKH; // Lưu ID khách hàng
                Session["Hoten"] = khachHang.HoTen;     // Lưu tên hiển thị

                return RedirectToAction("Index", "KhachHangController_65130449");
            }


            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "TrangChu_65130449");
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(KhachHang model, string XacNhanMatKhau)
        {
            // 1. Kiểm tra các trường bắt buộc
            if (string.IsNullOrEmpty(model.HoTen) ||
                string.IsNullOrEmpty(model.TenDangNhap) ||
                string.IsNullOrEmpty(model.MatKhau) ||
                string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc.");
                return View(model);
            }

            // 2. Kiểm tra xác nhận mật khẩu
            if (model.MatKhau != XacNhanMatKhau)
            {
                ModelState.AddModelError("XacNhanMatKhau", "Mật khẩu và xác nhận mật khẩu không khớp.");
                return View(model);
            }

            // 3. Kiểm tra trùng tên đăng nhập hoặc email
            if (db.KhachHangs.Any(kh => kh.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }
            if (db.KhachHangs.Any(kh => kh.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã được đăng ký.");
                return View(model);
            }

            // 4. Lưu vào database
            db.KhachHangs.Add(model);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "TrangChu_65130449");
        }
    }
}
