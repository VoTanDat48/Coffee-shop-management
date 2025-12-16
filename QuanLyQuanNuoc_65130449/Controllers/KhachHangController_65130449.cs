using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyQuanNuoc_65130449.Models;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class KhachHangController_65130449Controller : Controller
    {
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities();

        public ActionResult Index()
        {
            List<SanPham> featuredProducts = db.SanPhams
                                            // Sử dụng SqlFunctions.Rand() hoặc tương tự, 
                                            // nhưng cách đơn giản nhất là ORDER BY NEWID() 
                                            // nếu bạn dùng Database First với SQL Server
                                            .OrderBy(x => Guid.NewGuid())
                                            .Take(3)
                                            .ToList();

            // Truyền danh sách sản phẩm sang View
            return View(featuredProducts);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult MyAccount()
        {
            if (Session["MaKH"] == null)
            {
                // Chưa đăng nhập → chuyển về login
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var kh = db.KhachHangs.Find(maKH);

            if (kh == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "TrangChu_65130449");
            }

            return View(kh);
        }

        // GET: Chỉnh sửa thông tin
        public ActionResult EditAccount()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var kh = db.KhachHangs.Find(maKH);

            if (kh == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("MyAccount");
            }

            // Truyền TempData từ POST action nếu có
            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }

            return View(kh);
        }

        // POST: Lưu chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(KhachHang model, string MatKhauCu = "", string MatKhauMoi = "", string XacNhanMatKhau = "")
        {
            // 1. Kiểm tra session
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            // 2. Lấy thông tin khách hàng hiện tại từ DB
            var khachHangGoc = db.KhachHangs.Find(model.MaKH);

            if (khachHangGoc == null)
            {
                ModelState.AddModelError("", "Khách hàng không tồn tại hoặc phiên làm việc đã kết thúc.");
                return View(model);
            }

            // 3. KIỂM TRA THỦ CÔNG TẤT CẢ CÁC TRƯỜNG
            bool hasValidationError = false;

            if (string.IsNullOrWhiteSpace(model.HoTen))
            {
                ModelState.AddModelError("HoTen", "Họ tên không được để trống");
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không được để trống");
                hasValidationError = true;
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.SoDienThoai, @"^0\d{9,10}$"))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không hợp lệ.");
                hasValidationError = true;
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống");
                hasValidationError = true;
            }
            else if (!IsValidEmail(model.Email))
            {
                ModelState.AddModelError("Email", "Địa chỉ email không hợp lệ.");
                hasValidationError = true;
            }

            // 4. Xử lý đổi mật khẩu - PHẢI KHAI BÁO isPasswordChanging Ở ĐÂY
            bool isPasswordChanging = !string.IsNullOrWhiteSpace(MatKhauMoi);

            if (isPasswordChanging)
            {
                // Nếu đổi mật khẩu, tất cả các trường phải được nhập
                if (string.IsNullOrWhiteSpace(MatKhauCu))
                {
                    ModelState.AddModelError("MatKhauCu", "Vui lòng nhập mật khẩu cũ để đổi mật khẩu.");
                    hasValidationError = true;
                }
                else if (khachHangGoc.MatKhau != MatKhauCu)
                {
                    ModelState.AddModelError("MatKhauCu", "Mật khẩu cũ không đúng.");
                    hasValidationError = true;
                }

                if (string.IsNullOrWhiteSpace(MatKhauMoi))
                {
                    ModelState.AddModelError("MatKhauMoi", "Mật khẩu mới không được để trống");
                    hasValidationError = true;
                }
                else if (MatKhauMoi.Length < 6)
                {
                    ModelState.AddModelError("MatKhauMoi", "Mật khẩu mới phải có ít nhất 6 ký tự");
                    hasValidationError = true;
                }

                if (string.IsNullOrWhiteSpace(XacNhanMatKhau))
                {
                    ModelState.AddModelError("XacNhanMatKhau", "Vui lòng xác nhận mật khẩu mới");
                    hasValidationError = true;
                }
                else if (MatKhauMoi != XacNhanMatKhau)
                {
                    ModelState.AddModelError("XacNhanMatKhau", "Xác nhận mật khẩu mới không khớp.");
                    hasValidationError = true;
                }
            }

            // 5. NẾU CÓ LỖI, KHÔNG ĐƯỢC LƯU
            if (hasValidationError)
            {
                model.MatKhau = khachHangGoc.MatKhau;
                model.TenDangNhap = khachHangGoc.TenDangNhap; // Gán lại để không bị null
                ViewBag.Error = "Lưu thất bại: Vui lòng kiểm tra lại thông tin đã nhập.";
                return View(model);
            }

            // 6. NẾU KHÔNG CÓ LỖI, THỰC HIỆN CẬP NHẬT
            try
            {
                // Cập nhật thông tin cơ bản
                khachHangGoc.HoTen = model.HoTen.Trim();
                khachHangGoc.SoDienThoai = model.SoDienThoai.Trim();
                khachHangGoc.Email = model.Email.Trim();

                // Cập nhật mật khẩu nếu có thay đổi - DÙNG BIẾN isPasswordChanging Ở ĐÂY
                if (isPasswordChanging)
                {
                    khachHangGoc.MatKhau = MatKhauMoi.Trim();
                }

                // Lưu thay đổi
                db.SaveChanges();

                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("EditAccount");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Xử lý lỗi chi tiết
                var errorMessages = new List<string>();
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        string propertyName = validationError.PropertyName;
                        string errorMessage = validationError.ErrorMessage;

                        ModelState.AddModelError(propertyName, errorMessage);
                        errorMessages.Add($"{propertyName}: {errorMessage}");
                    }
                }

                model.MatKhau = khachHangGoc.MatKhau;
                model.TenDangNhap = khachHangGoc.TenDangNhap;
                ViewBag.Error = "Lưu thất bại: " + (errorMessages.Any() ? string.Join("; ", errorMessages) : "Dữ liệu không hợp lệ.");
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}");
                model.MatKhau = khachHangGoc.MatKhau;
                model.TenDangNhap = khachHangGoc.TenDangNhap;
                ViewBag.Error = "Lưu thất bại: Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        // Hàm kiểm tra email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }


        public ActionResult Menu_KH(string search, string categoryId, int page = 1, int pageSize = 9)
        {
            // Chuẩn bị dữ liệu lọc
            ViewBag.Categories = db.DanhMucs
                                   .OrderBy(dm => dm.TenDanhMuc)
                                   .ToList();
            ViewBag.Search = search;
            ViewBag.SelectedCategoryId = categoryId;

            IQueryable<SanPham> query = db.SanPhams;

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(sp => sp.TenSP.Contains(keyword));
            }

            // Lọc theo danh mục
            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(sp => sp.MaDanhMuc == categoryId);
            }

            // Thông tin phân trang
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var sanPhams = query
                .OrderBy(sp => sp.TenSP)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(sanPhams);
        }

    }
}