using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyQuanNuoc_65130449.Models;
using System.Data.Entity;

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

        // Helper method để generate ID (phiên bản mới, dùng Func kiểm tra trùng ID)
        private string GenerateNextId(string prefix, Func<string, bool> existsFunc, int offset = 0)
        {
            int counter = 1 + offset; // Bắt đầu từ 1 cộng với số thứ tự trong vòng lặp
            string newId;
            do
            {
                newId = prefix + counter.ToString("D4");
                counter++;
            } while (existsFunc(newId) && counter < 10000);
            return newId;
        }

        // Helper cũ (giữ lại để tránh lỗi compile ở những chỗ đang gọi theo kiểu cũ:
        // GenerateNextId(string tableName, string columnName, string prefix, int length))
        // Bạn có thể xoá dần các chỗ gọi cũ và chuyển sang dùng hàm mới bên trên.
        private string GenerateNextId(string tableName, string columnName, string prefix, int length)
        {
            // Xác định hàm existsFunc theo tên bảng
            Func<string, bool> existsFunc;
            switch (tableName)
            {
                case "GioHang":
                    existsFunc = id => db.GioHangs.Any(gh => gh.MaGioHang == id);
                    break;
                case "ChiTietGioHang":
                    existsFunc = id => db.ChiTietGioHangs.Any(ct => ct.MaCTGioHang == id);
                    break;
                case "DonHang":
                    existsFunc = id => db.DonHangs.Any(dh => dh.MaDonHang == id);
                    break;
                case "ChiTietDonHang":
                    existsFunc = id => db.ChiTietDonHangs.Any(ct => ct.MaCTDH == id);
                    break;
                default:
                    // Mặc định: không kiểm tra trùng (ít dùng)
                    existsFunc = id => false;
                    break;
            }

            // Tính số chữ số cho phần số, nếu length truyền vào không hợp lệ thì fallback 4
            int numberLength = length - prefix.Length;
            if (numberLength <= 0) numberLength = 4;

            int counter = 1;
            string newId;
            do
            {
                newId = prefix + counter.ToString("D" + numberLength);
                counter++;
            } while (existsFunc(newId) && counter < 10000);

            return newId;
        }

        // GET: Giỏ hàng
        public ActionResult Cart()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKH == maKH);

            if (gioHang == null)
            {
                ViewBag.Message = "Giỏ hàng của bạn đang trống.";
                return View(new List<ChiTietGioHang>());
            }

            var chiTietGioHangs = db.ChiTietGioHangs
                .Include(ct => ct.SanPham)
                .Where(ct => ct.MaGioHang == gioHang.MaGioHang)
                .ToList();

            if (!chiTietGioHangs.Any())
            {
                ViewBag.Message = "Giỏ hàng của bạn đang trống.";
                return View(new List<ChiTietGioHang>());
            }

            decimal tongTien = chiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaLucThem);
            ViewBag.TongTien = tongTien;

            return View(chiTietGioHangs);
        }

        // POST: Thêm sản phẩm vào giỏ hàng
        public ActionResult AddToCart(string productId, int quantity = 1)
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var sanPham = db.SanPhams.Find(productId);

            if (sanPham == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Menu_KH");
            }

            try
            {
                // Tìm hoặc tạo giỏ hàng
                var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKH == maKH);
                if (gioHang == null)
                {
                    string maGioHang = GenerateNextId("GH", id => db.GioHangs.Any(gh => gh.MaGioHang == id));
                    gioHang = new GioHang
                    {
                        MaGioHang = maGioHang,
                        MaKH = maKH,
                        NgayCapNhat = DateTime.Now
                    };
                    db.GioHangs.Add(gioHang);
                    db.SaveChanges();
                }

                // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
                var chiTiet = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSP == productId);
                if (chiTiet != null)
                {
                    // Cập nhật số lượng
                    chiTiet.SoLuong += quantity;
                }
                else
                {
                    // Thêm mới
                    string maCTGioHang = GenerateNextId("CTGH", id => db.ChiTietGioHangs.Any(ct => ct.MaCTGioHang == id));
                    chiTiet = new ChiTietGioHang
                    {
                        MaCTGioHang = maCTGioHang,
                        MaGioHang = gioHang.MaGioHang,
                        MaSP = productId,
                        SoLuong = quantity,
                        DonGiaLucThem = sanPham.DonGia
                    };
                    db.ChiTietGioHangs.Add(chiTiet);
                }

                gioHang.NgayCapNhat = DateTime.Now;
                db.SaveChanges();

                TempData["CartMsg"] = "Đã thêm sản phẩm vào giỏ hàng!";

            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng.";
            }

            return RedirectToAction("Menu_KH");
        }

        // POST: Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public ActionResult UpdateCart(string maCTGioHang, int soLuong)
        {
            if (Session["MaKH"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            try
            {
                var chiTiet = db.ChiTietGioHangs.Find(maCTGioHang);
                if (chiTiet == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                // Kiểm tra quyền sở hữu giỏ hàng
                string maKH = Session["MaKH"].ToString();
                if (chiTiet.GioHang.MaKH != maKH)
                {
                    return Json(new { success = false, message = "Bạn không có quyền cập nhật giỏ hàng này." });
                }

                if (soLuong <= 0)
                {
                    // Xóa nếu số lượng <= 0
                    db.ChiTietGioHangs.Remove(chiTiet);
                }
                else
                {
                    chiTiet.SoLuong = soLuong;
                }

                chiTiet.GioHang.NgayCapNhat = DateTime.Now;
                db.SaveChanges();

                // Tính lại tổng tiền
                var gioHang = db.GioHangs.Find(chiTiet.MaGioHang);
                decimal tongTien = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaLucThem);
                decimal thanhTien = soLuong > 0 ? soLuong * chiTiet.DonGiaLucThem : 0;

                return Json(new { success = true, tongTien = tongTien, thanhTien = thanhTien });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật giỏ hàng." });
            }
        }

        // POST: Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public ActionResult RemoveFromCart(string maCTGioHang)
        {
            if (Session["MaKH"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            try
            {
                var chiTiet = db.ChiTietGioHangs.Find(maCTGioHang);
                if (chiTiet == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                // Kiểm tra quyền sở hữu giỏ hàng
                string maKH = Session["MaKH"].ToString();
                if (chiTiet.GioHang.MaKH != maKH)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xóa sản phẩm này." });
                }

                string maGioHang = chiTiet.MaGioHang;
                db.ChiTietGioHangs.Remove(chiTiet);

                var gioHang = db.GioHangs.Find(maGioHang);
                if (gioHang != null)
                {
                    gioHang.NgayCapNhat = DateTime.Now;
                }

                db.SaveChanges();

                // Tính lại tổng tiền
                decimal tongTien = gioHang.ChiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaLucThem);

                return Json(new { success = true, tongTien = tongTien });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm." });
            }
        }

        // GET: Checkout
        public ActionResult Checkout()
        {
            if (Session["MaKH"] == null)
                return RedirectToAction("Login", "AccountController_65130449");

            string maKH = Session["MaKH"].ToString();
            var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKH == maKH);

            if (gioHang == null || !db.ChiTietGioHangs.Any(ct => ct.MaGioHang == gioHang.MaGioHang))
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Cart");
            }

            PrepareCheckoutData(maKH, gioHang);
            return View();
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(string diaChiGiaoHang, decimal phiVanChuyen = 15000)
        {
            string maKH = Session["MaKH"]?.ToString();
            if (maKH == null) return RedirectToAction("Login", "AccountController_65130449");

            var gioHang = db.GioHangs.FirstOrDefault(gh => gh.MaKH == maKH);

            if (string.IsNullOrWhiteSpace(diaChiGiaoHang))
            {
                TempData["Error"] = "Vui lòng nhập địa chỉ giao hàng cụ thể.";
                PrepareCheckoutData(maKH, gioHang);
                return View();
            }

            // Sử dụng Transaction để đảm bảo an toàn dữ liệu
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Tạo mã đơn hàng
                    string maDonHang = GenerateNextId("DH", id => db.DonHangs.Any(dh => dh.MaDonHang == id));

                    // Lấy danh sách chi tiết giỏ hàng
                    var chiTietGioHangs = db.ChiTietGioHangs
                        .Where(ct => ct.MaGioHang == gioHang.MaGioHang)
                        .ToList();

                    decimal tongTienSanPham = chiTietGioHangs.Sum(ct => ct.SoLuong * ct.DonGiaLucThem);

                    var donHang = new DonHang
                    {
                        MaDonHang = maDonHang,
                        MaKH = maKH,
                        NgayDat = DateTime.Now,
                        TrangThai = "CHODUYET",
                        DiaChiGiaoHang = diaChiGiaoHang.Trim(),
                        PhiVanChuyen = phiVanChuyen,
                        TongTien = tongTienSanPham + phiVanChuyen, // Tổng tiền bao gồm phí ship
                        DaThanhToan = false
                    };
                    db.DonHangs.Add(donHang);

                    // 2. XỬ LÝ CHI TIẾT ĐƠN HÀNG (Sửa lỗi Lambda Statement Body)
                    // Lấy danh sách ID về RAM trước để xử lý chuỗi
                    var allCtdhIds = db.ChiTietDonHangs
                        .Select(c => c.MaCTDH)
                        .Where(id => id.StartsWith("CTDH"))
                        .AsEnumerable(); // Quan trọng: Đưa về xử lý trên C#

                    int lastNum = allCtdhIds
                        .Select(id => {
                            int n;
                            return (id.Length > 4 && int.TryParse(id.Substring(4), out n)) ? n : 0;
                        })
                        .DefaultIfEmpty(0)
                        .Max();

                    int i = 1;
                    foreach (var ctgh in chiTietGioHangs)
                    {
                        // Tạo mã CTDH mới: CTDH0001, CTDH0002...
                        string maCTDH = "CTDH" + (lastNum + i).ToString("D4");

                        db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaCTDH = maCTDH,
                            MaDonHang = maDonHang,
                            MaSP = ctgh.MaSP,
                            SoLuong = ctgh.SoLuong,
                            DonGia = ctgh.DonGiaLucThem
                        });

                        // Xóa món này khỏi giỏ hàng
                        db.ChiTietGioHangs.Remove(ctgh);
                        i++;
                    }

                    // Xóa đầu mục giỏ hàng
                    db.GioHangs.Remove(gioHang);

                    // Lưu tất cả thay đổi
                    db.SaveChanges();

                    // Xác nhận hoàn tất giao dịch
                    transaction.Commit();

                    TempData["Success"] = "Đặt hàng thành công!";
                    return RedirectToAction("MyOrders");
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, hủy bỏ mọi thay đổi đã thực hiện trong try
                    transaction.Rollback();

                    TempData["Error"] = "Lỗi hệ thống: " + (ex.InnerException?.InnerException?.Message ?? ex.Message);
                    PrepareCheckoutData(maKH, gioHang);
                    return View();
                }
            }
        }

        // Hàm bổ trợ nạp dữ liệu cho View (Giữ nguyên)
        private void PrepareCheckoutData(string maKH, GioHang gioHang)
        {
            ViewBag.KhachHang = db.KhachHangs.Find(maKH);

            if (gioHang != null)
            {
                var chiTiet = db.ChiTietGioHangs
                                .Include(ct => ct.SanPham)
                                .Where(ct => ct.MaGioHang == gioHang.MaGioHang)
                                .ToList();

                ViewBag.ChiTietGioHangs = chiTiet;
                decimal tongTien = chiTiet.Sum(ct => ct.SoLuong * ct.DonGiaLucThem);
                ViewBag.TongTien = tongTien;
                ViewBag.PhiVanChuyen = 15000;
                ViewBag.TongThanhToan = tongTien + 15000;
            }
        }


        // GET: Xem đơn hàng
        public ActionResult MyOrders()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var donHangs = db.DonHangs
                .Where(dh => dh.MaKH == maKH)
                .OrderByDescending(dh => dh.NgayDat)
                .ToList();

            // Tạo dictionary để map trạng thái
            ViewBag.TrangThaiDict = new Dictionary<string, string>
            {
                { "CHODUYET", "Chờ duyệt" },
                { "DADUYET", "Đã duyệt" },
                { "DANGPHACHE", "Đang pha chế" },
                { "DANGGIAO", "Đang giao" },
                { "HOANTHANH", "Hoàn thành" },
                { "HUY", "Đã hủy" }
            };

            return View(donHangs);
        }

        // GET: Chi tiết đơn hàng
        public ActionResult OrderDetails(string id)
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "AccountController_65130449");
            }

            string maKH = Session["MaKH"].ToString();
            var donHang = db.DonHangs
                .Include(dh => dh.ChiTietDonHangs.Select(ct => ct.SanPham))
                .FirstOrDefault(dh => dh.MaDonHang == id && dh.MaKH == maKH);

            if (donHang == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyOrders");
            }

            ViewBag.TrangThaiDict = new Dictionary<string, string>
            {
                { "CHODUYET", "Chờ duyệt" },
                { "DADUYET", "Đã duyệt" },
                { "DANGPHACHE", "Đang pha chế" },
                { "DANGGIAO", "Đang giao" },
                { "HOANTHANH", "Hoàn thành" },
                { "HUY", "Đã hủy" }
            };

            return View(donHang);
        }
    }
}