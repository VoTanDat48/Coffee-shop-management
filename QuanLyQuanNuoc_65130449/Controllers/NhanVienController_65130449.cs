using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyQuanNuoc_65130449.Models;
using System.Data.Entity;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class NhanVienController_65130449Controller : Controller
    {
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities();

        // Kiểm tra nhân viên đã đăng nhập
        private bool IsEmployeeLoggedIn()
        {
            return Session["MaNV"] != null && Session["UserRole"] != null;
        }

        // Kiểm tra vai trò nhân viên
        private bool IsEmployeeRole(int vaiTro)
        {
            if (Session["VaiTro"] == null) return false;
            int sessionVaiTro = (int)Session["VaiTro"];
            return sessionVaiTro == vaiTro || sessionVaiTro == 3; // 3 là Quản lý có thể làm tất cả
        }

        // GET: Xem hồ sơ nhân viên
        public ActionResult MyAccount()
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maNV = Session["MaNV"].ToString();
            var nv = db.NhanViens.Find(maNV);

            if (nv == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin nhân viên.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            return View(nv);
        }

        // GET: Chỉnh sửa thông tin nhân viên
        public ActionResult EditAccount()
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maNV = Session["MaNV"].ToString();
            var nv = db.NhanViens.Find(maNV);

            if (nv == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin nhân viên.";
                return RedirectToAction("MyAccount");
            }

            if (TempData["Success"] != null)
            {
                ViewBag.Success = TempData["Success"];
            }

            return View(nv);
        }

        // POST: Lưu chỉnh sửa thông tin nhân viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(NhanVien model, string MatKhauCu = "", string MatKhauMoi = "", string XacNhanMatKhau = "")
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var nhanVienGoc = db.NhanViens.Find(model.MaNV);

            if (nhanVienGoc == null)
            {
                ModelState.AddModelError("", "Nhân viên không tồn tại hoặc phiên làm việc đã kết thúc.");
                return View(model);
            }

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

            bool isPasswordChanging = !string.IsNullOrWhiteSpace(MatKhauMoi);

            if (isPasswordChanging)
            {
                if (string.IsNullOrWhiteSpace(MatKhauCu))
                {
                    ModelState.AddModelError("MatKhauCu", "Vui lòng nhập mật khẩu cũ để đổi mật khẩu.");
                    hasValidationError = true;
                }
                else if (nhanVienGoc.MatKhau != MatKhauCu)
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

            if (hasValidationError)
            {
                model.MatKhau = nhanVienGoc.MatKhau;
                model.TenDangNhap = nhanVienGoc.TenDangNhap;
                ViewBag.Error = "Lưu thất bại: Vui lòng kiểm tra lại thông tin đã nhập.";
                return View(model);
            }

            try
            {
                nhanVienGoc.HoTen = model.HoTen.Trim();
                nhanVienGoc.SoDienThoai = model.SoDienThoai.Trim();

                if (isPasswordChanging)
                {
                    nhanVienGoc.MatKhau = MatKhauMoi.Trim();
                }

                db.SaveChanges();

                // Cập nhật Session
                Session["HoTen"] = nhanVienGoc.HoTen;

                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("EditAccount");
            }
            catch (Exception ex)
            {
                model.MatKhau = nhanVienGoc.MatKhau;
                model.TenDangNhap = nhanVienGoc.TenDangNhap;
                ViewBag.Error = "Lưu thất bại: Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                return View(model);
            }
        }

        // GET: Duyệt đơn hàng (chỉ nhân viên duyệt - VaiTro = 1)
        public ActionResult DuyetDon()
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(1))
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            ViewBag.Title = "Duyệt đơn hàng";

            // Lấy danh sách đơn hàng chờ duyệt
            var donHangs = db.DonHangs
                .Include(dh => dh.KhachHang)
                .Where(dh => dh.TrangThai == "CHODUYET")
                .OrderByDescending(dh => dh.NgayDat)
                .ToList();

            ViewBag.TrangThaiDict = new Dictionary<string, string>
            {
                { "CHODUYET", "Chờ duyệt" },
                { "DANGPHACHE", "Đang pha chế" },
                { "DANGGIAO", "Đang giao" },
                { "HOANTHANH", "Hoàn thành" },
                { "HUY", "Đã hủy" }
            };

            return View(donHangs);
        }

        // GET: Chi tiết đơn hàng để duyệt
        public ActionResult ChiTietDonHang(string id)
        {
            if (!IsEmployeeLoggedIn())
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var donHang = db.DonHangs
                .Include(dh => dh.KhachHang)
                .Include(dh => dh.ChiTietDonHangs.Select(ct => ct.SanPham))
                .FirstOrDefault(dh => dh.MaDonHang == id);

            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("DuyetDon");
            }

            ViewBag.TrangThaiDict = new Dictionary<string, string>
            {
                { "CHODUYET", "Chờ duyệt" },
                { "DANGPHACHE", "Đang pha chế" },
                { "DANGGIAO", "Đang giao" },
                { "HOANTHANH", "Hoàn thành" },
                { "HUY", "Đã hủy" }
            };

            return View(donHang);
        }

        // POST: Duyệt đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DuyetDonHang(string maDonHang)
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(1))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var donHang = db.DonHangs.Find(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("DuyetDon");
            }

            if (donHang.TrangThai != "CHODUYET")
            {
                TempData["Error"] = "Đơn hàng này đã được xử lý.";
                return RedirectToAction("DuyetDon");
            }

            try
            {
                string maNV = Session["MaNV"].ToString();
                donHang.TrangThai = "DANGPHACHE";
                donHang.NhanVienDuyet = maNV;
                db.SaveChanges();

                TempData["Success"] = "Đã duyệt đơn hàng thành công!";
                return RedirectToAction("DuyetDon");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi duyệt đơn hàng: " + ex.Message;
                return RedirectToAction("ChiTietDonHang", new { id = maDonHang });
            }
        }

        // POST: Từ chối đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TuChoiDonHang(string maDonHang)
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(1))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var donHang = db.DonHangs.Find(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("DuyetDon");
            }

            if (donHang.TrangThai != "CHODUYET")
            {
                TempData["Error"] = "Đơn hàng này đã được xử lý.";
                return RedirectToAction("DuyetDon");
            }

            try
            {
                donHang.TrangThai = "HUY";
                db.SaveChanges();

                TempData["Success"] = "Đã từ chối đơn hàng.";
                return RedirectToAction("DuyetDon");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi từ chối đơn hàng: " + ex.Message;
                return RedirectToAction("ChiTietDonHang", new { id = maDonHang });
            }
        }

        // GET: Nhân viên giao hàng - Xem danh sách đơn đã duyệt
        public ActionResult GiaoHang()
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(2))
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            ViewBag.Title = "Giao hàng";

            string maNV = Session["MaNV"].ToString();

            // Lấy đơn hàng đã duyệt (DANGPHACHE) nhưng CHƯA được nhận giao (NhanVienGiao == null)
            var availableOrders = db.DonHangs
                .Include(dh => dh.KhachHang)
                .Where(dh => dh.TrangThai == "DANGPHACHE" && dh.NhanVienGiao == null)
                .OrderByDescending(dh => dh.NgayDat)
                .ToList();

            // Lấy đơn hàng đang giao của nhân viên hiện tại (đã nhận để giao)
            var myOrders = db.DonHangs
                .Include(dh => dh.KhachHang)
                .Where(dh => dh.TrangThai == "DANGGIAO" && dh.NhanVienGiao == maNV)
                .OrderByDescending(dh => dh.NgayDat)
                .ToList();

            // Gộp cả 2 danh sách để truyền vào View (View sẽ tự phân loại)
            var allOrders = availableOrders.Concat(myOrders).ToList();

            ViewBag.TrangThaiDict = new Dictionary<string, string>
            {
                { "CHODUYET", "Chờ duyệt" },
                { "DANGPHACHE", "Đang pha chế" },
                { "DANGGIAO", "Đang giao" },
                { "HOANTHANH", "Hoàn thành" },
                { "HUY", "Đã hủy" }
            };

            // Truyền MaNV để View có thể lọc đúng
            ViewBag.MaNV = maNV;

            return View(allOrders);
        }

        // POST: Nhận đơn hàng để giao (chuyển từ DADUYET sang DANGGIAO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NhanDonHang(string maDonHang)
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(2))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var donHang = db.DonHangs.Find(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("GiaoHang");
            }

            if (donHang.TrangThai != "DANGPHACHE")
            {
                TempData["Error"] = "Đơn hàng này không thể nhận giao.";
                return RedirectToAction("GiaoHang");
            }

            try
            {
                string maNV = Session["MaNV"].ToString();
                donHang.TrangThai = "DANGGIAO";
                donHang.NhanVienGiao = maNV;
                db.SaveChanges();

                TempData["Success"] = "Đã nhận đơn hàng thành công!";
                return RedirectToAction("GiaoHang");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi nhận đơn hàng: " + ex.Message;
                return RedirectToAction("GiaoHang");
            }
        }

        // POST: Hoàn thành giao hàng (chuyển từ DANGGIAO sang HOANTHANH)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HoanThanhGiaoHang(string maDonHang)
        {
            if (!IsEmployeeLoggedIn() || !IsEmployeeRole(2))
            {
                TempData["Error"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction("Login", "AccountController_65130449");
            }

            var donHang = db.DonHangs.Find(maDonHang);
            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("GiaoHang");
            }

            string maNV = Session["MaNV"].ToString();
            if (donHang.TrangThai != "DANGGIAO" || donHang.NhanVienGiao != maNV)
            {
                TempData["Error"] = "Bạn không thể hoàn thành đơn hàng này.";
                return RedirectToAction("GiaoHang");
            }

            try
            {
                donHang.TrangThai = "HOANTHANH";
                donHang.DaThanhToan = true; // Thanh toán khi nhận tiền mặt
                donHang.NgayThanhToan = DateTime.Now;
                db.SaveChanges();

                TempData["Success"] = "Đã hoàn thành giao hàng thành công!";
                return RedirectToAction("GiaoHang");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi hoàn thành giao hàng: " + ex.Message;
                return RedirectToAction("GiaoHang");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
