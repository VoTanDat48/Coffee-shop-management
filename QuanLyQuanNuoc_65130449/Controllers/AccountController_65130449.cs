using QuanLyQuanNuoc_65130449.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class AccountController_65130449Controller : Controller
    {
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities();

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

            // 1. KIỂM TRA NHÂN VIÊN
            var nhanVien = db.NhanViens
                             .FirstOrDefault(nv => nv.TenDangNhap == TenDangNhap && nv.MatKhau == MatKhau);

            if (nhanVien != null)
            {
                // Thiết lập Cookie đăng nhập
                FormsAuthentication.SetAuthCookie(nhanVien.TenDangNhap, false);

                // QUAN TRỌNG: Lưu vai trò Admin/NhanVien vào Session để bảo mật trang quản lý
                Session["UserRole"] = "Admin";
                Session["MaNV"] = nhanVien.MaNV;
                Session["HoTen"] = nhanVien.HoTen;
                Session["VaiTro"] = nhanVien.VaiTro;

                // Phân quyền điều hướng trang sau khi đăng nhập
                switch (nhanVien.VaiTro)
                {
                    case 3: // Quản lý
                        return RedirectToAction("Index", "AdminController_65130449");
                    case 1: // NV Duyệt
                        return RedirectToAction("Processing", "Employee");
                    case 2: // NV Giao hàng
                        return RedirectToAction("Delivery", "Employee");
                    default:
                        return RedirectToAction("Index", "KhachHangController_65130449");
                }
            }

            // --- BƯỚC 2: NẾU KHÔNG PHẢI NHÂN VIÊN, KIỂM TRA BẢNG KHÁCH HÀNG ---
            var khachHang = db.KhachHangs.FirstOrDefault(kh => kh.TenDangNhap == TenDangNhap && kh.MatKhau == MatKhau);
            if (khachHang != null)
            {
                FormsAuthentication.SetAuthCookie(khachHang.TenDangNhap, false);
                Session["UserRole"] = "Customer";
                Session["MaKH"] = khachHang.MaKH;
                Session["HoTen"] = khachHang.HoTen;

                // Ưu tiên quay lại trang khách hàng đang xem dở
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                
                return RedirectToAction("Index", "KhachHangController_65130449");
            }

            // --- BƯỚC 3: THẤT BẠI ---
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();
            Session.Clear(); // Xóa sạch Session (Role, MaKH, HoTen...)
            Session.Abandon();
            return RedirectToAction("Index", "TrangChuController_65130449");
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
            if (string.IsNullOrEmpty(model.HoTen) || string.IsNullOrEmpty(model.TenDangNhap) ||
                string.IsNullOrEmpty(model.MatKhau) || string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin bắt buộc.");
                return View(model);
            }

            if (model.MatKhau != XacNhanMatKhau)
            {
                ModelState.AddModelError("XacNhanMatKhau", "Mật khẩu xác nhận không khớp.");
                return View(model);
            }

            if (db.KhachHangs.Any(kh => kh.TenDangNhap == model.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }

            // Tự động tạo mã KH (Giữ nguyên logic của bạn)
            var lastKhachHang = db.KhachHangs.OrderByDescending(kh => kh.MaKH).FirstOrDefault();
            string newMaKH = "KH0001";
            if (lastKhachHang != null && lastKhachHang.MaKH.StartsWith("KH"))
            {
                string numberPart = lastKhachHang.MaKH.Substring(2);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    newMaKH = "KH" + (lastNumber + 1).ToString("D4");
                }
            }
            model.MaKH = newMaKH;

            db.KhachHangs.Add(model);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }


        // 1. Giao diện nhập Email
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // 2. Xử lý gửi mail OTP
        [HttpPost]
        public ActionResult ForgotPassword(string Email)
        {
            var kh = db.KhachHangs.FirstOrDefault(x => x.Email == Email);
            if (kh == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                return View();
            }

            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;
            Session["ResetEmail"] = Email;

            try
            {
                // CHÚ Ý: Đã sửa lại định dạng email tại đây
                var fromAddress = new MailAddress("votandat4825@gmail.com", "Windy Coffee");
                var toAddress = new MailAddress(Email.Trim());
                string fromPassword = "htap ygmb vflq orio";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Mã xác nhận đổi mật khẩu - Windy Coffee",
                    Body = $"Mã OTP của bạn là: {otp}. Đừng chia sẻ mã này cho ai."
                })
                {
                    smtp.Send(message);
                }
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi gửi mail: " + ex.Message);
                return View();
            }
        }

        // 3. Giao diện đổi mật khẩu mới
        [HttpGet]
        public ActionResult ResetPassword()
        {
            if (Session["OTP"] == null) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public ActionResult ResetPassword(string OTPConfirm, string MatKhauMoi, string XacNhanMatKhau)
        {
            if (OTPConfirm != Session["OTP"].ToString())
            {
                ModelState.AddModelError("", "Mã OTP không chính xác.");
                return View();
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            string email = Session["ResetEmail"].ToString();
            var kh = db.KhachHangs.FirstOrDefault(x => x.Email == email);

            if (kh != null)
            {
                kh.MatKhau = MatKhauMoi; // Cập nhật pass mới
                db.SaveChanges();

                // Xóa Session sau khi xong
                Session.Remove("OTP");
                Session.Remove("ResetEmail");

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Login");
            }

            return View();
        }
    }
}