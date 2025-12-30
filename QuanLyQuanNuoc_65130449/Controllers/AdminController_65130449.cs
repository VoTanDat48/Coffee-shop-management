using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyQuanNuoc_65130449.Models;
using System.IO;
using System.Data.Entity;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    [AdminAuthorize]
    public class AdminController_65130449Controller : Controller
    {
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities db = 
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuocWindy_65130449Entities();

        // GET: Dashboard
        public ActionResult Index()
        {
            ViewBag.Title = "Dashboard";
            ViewBag.TongSanPham = db.SanPhams.Count();
            ViewBag.TongDanhMuc = db.DanhMucs.Count();
            ViewBag.TongNhanVien = db.NhanViens.Count();
            ViewBag.TongDonHang = db.DonHangs.Count();
            
            // Lấy danh sách sản phẩm và danh mục
            ViewBag.SanPhams = db.SanPhams.Take(10).ToList();
            ViewBag.DanhMucs = db.DanhMucs.ToList();
            
            return View();
        }
        
        // ========== QUẢN LÝ SẢN PHẨM ==========
        public ActionResult DS_SP(int? page, int? pageSize, string categoryId)
        {
            ViewBag.Title = "Quản lý Sản phẩm";
            
            // Lấy giá trị từ query string nếu không có từ parameter
            if (string.IsNullOrEmpty(categoryId))
            {
                categoryId = Request.QueryString["categoryId"];
            }
            
            int currentPage = page ?? 1;
            int currentPageSize = pageSize ?? 5;
            
            IQueryable<SanPham> query = db.SanPhams.Include("DanhMuc");
            
            // Lọc theo danh mục nếu có
            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(sp => sp.MaDanhMuc == categoryId);
            }
            
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / currentPageSize);
            
            var sanPhams = query
                .OrderBy(sp => sp.MaSP)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToList();
            
            ViewBag.DanhMucs = db.DanhMucs.OrderBy(dm => dm.MaDanhMuc).ToList();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = currentPageSize;
            ViewBag.TotalItems = totalItems;
            
            return View(sanPhams);
        }

        [HttpGet]
        public ActionResult Create_SP()
        {
            ViewBag.Title = "Thêm sản phẩm";
            ViewBag.DanhMucs = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create_SP(SanPham model, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                // Tự động tạo mã sản phẩm mới (SP001, SP002, ...)
                var lastSanPham = db.SanPhams.OrderByDescending(sp => sp.MaSP).FirstOrDefault();
                string newMaSP = "SP001";
                
                if (lastSanPham != null && lastSanPham.MaSP.StartsWith("SP"))
                {
                    // Lấy số từ mã cuối cùng
                    string numberPart = lastSanPham.MaSP.Substring(2);
                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        int newNumber = lastNumber + 1;
                        newMaSP = "SP" + newNumber.ToString("D3"); // D3 để format 001, 002, ...
                    }
                }
                
                model.MaSP = newMaSP;

                // Upload ảnh
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileUpload.FileName);
                    string extension = Path.GetExtension(fileName);
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    
                    if (allowedExtensions.Contains(extension.ToLower()))
                    {
                        fileName = Guid.NewGuid().ToString() + extension;
                        string path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                        fileUpload.SaveAs(path);
                        model.HinhAnh = "/Images/" + fileName;
                    }
                }

                db.SanPhams.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Thêm sản phẩm thành công!";
                return RedirectToAction("DS_SP");
            }

            ViewBag.DanhMucs = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", model.MaDanhMuc);
            return View(model);
        }

        [HttpGet]
        public ActionResult Edit_SP(string id)
        {
            ViewBag.Title = "Chỉnh sửa sản phẩm";
            var sp = db.SanPhams.Find(id);
            if (sp == null) return HttpNotFound();
            
            ViewBag.DanhMucs = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", sp.MaDanhMuc);
            return View(sp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit_SP(SanPham model, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                var sp = db.SanPhams.Find(model.MaSP);
                if (sp == null) return HttpNotFound();

                // Upload ảnh mới nếu có
                if (fileUpload != null && fileUpload.ContentLength > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(sp.HinhAnh))
                    {
                        string oldPath = Server.MapPath(sp.HinhAnh);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    string fileName = Path.GetFileName(fileUpload.FileName);
                    string extension = Path.GetExtension(fileName);
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    
                    if (allowedExtensions.Contains(extension.ToLower()))
                    {
                        fileName = Guid.NewGuid().ToString() + extension;
                        string path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                        fileUpload.SaveAs(path);
                        sp.HinhAnh = "/Images/" + fileName;
                    }
                }

                sp.TenSP = model.TenSP;
                sp.DonGia = model.DonGia;
                sp.MaDanhMuc = model.MaDanhMuc;

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("DS_SP");
            }

            ViewBag.DanhMucs = new SelectList(db.DanhMucs, "MaDanhMuc", "TenDanhMuc", model.MaDanhMuc);
            return View(model);
        }

        [HttpPost]
        public ActionResult Delete_SP(string id)
        {
            var sp = db.SanPhams.Find(id);
            if (sp == null) return HttpNotFound();

            // Xóa ảnh nếu có
            if (!string.IsNullOrEmpty(sp.HinhAnh))
            {
                string path = Server.MapPath(sp.HinhAnh);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }

            db.SanPhams.Remove(sp);
            db.SaveChanges();
            TempData["Success"] = "Xóa sản phẩm thành công!";
            return RedirectToAction("DS_SP");
        }
        public ActionResult TimKiemSP(string search)
        {
            ViewBag.Title = "Tìm kiếm Sản phẩm";
            ViewBag.Search = search;
            
            IQueryable<SanPham> query = db.SanPhams.Include("DanhMuc");
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(sp => sp.TenSP.Contains(keyword) || 
                                         (sp.DanhMuc != null && sp.DanhMuc.TenDanhMuc.Contains(keyword)));
            }
            
            var sanPhams = query.OrderBy(sp => sp.TenSP).ToList();
            return View(sanPhams);
        }

        // ========== QUẢN LÝ DANH MỤC ==========
        public ActionResult DS_DanhMuc()
        {
            ViewBag.Title = "Quản lý Danh mục";
            // Sắp xếp theo MaDanhMuc từ 1 đến 4
            var danhMucs = db.DanhMucs.OrderBy(dm => dm.MaDanhMuc).ToList();
            return View(danhMucs);
        }

        [HttpPost]
        public ActionResult Create_DanhMuc(string TenDanhMuc)
        {
            if (!string.IsNullOrEmpty(TenDanhMuc))
            {
                // Tự động tạo mã danh mục mới (DM01, DM02, ...)
                var lastDanhMuc = db.DanhMucs.OrderByDescending(DM => DM.MaDanhMuc).FirstOrDefault();
                string newMaDanhMuc = "DM01";
                
                if (lastDanhMuc != null && lastDanhMuc.MaDanhMuc.StartsWith("DM"))
                {
                    // Lấy số từ mã cuối cùng
                    string numberPart = lastDanhMuc.MaDanhMuc.Substring(2);
                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        int newNumber = lastNumber + 1;
                        newMaDanhMuc = "DM" + newNumber.ToString("D2"); // D2 để format 01, 02, ...
                    }
                }
                
                var dm = new DanhMuc 
                { 
                    MaDanhMuc = newMaDanhMuc,
                    TenDanhMuc = TenDanhMuc 
                };
                db.DanhMucs.Add(dm);
                db.SaveChanges();
                TempData["Success"] = "Thêm danh mục thành công!";
            }
            return RedirectToAction("DS_DanhMuc");
        }

        [HttpPost]
        public ActionResult Edit_DanhMuc(string id, string TenDanhMuc)
        {
            var dm = db.DanhMucs.Find(id);
            if (dm != null && !string.IsNullOrEmpty(TenDanhMuc))
            {
                dm.TenDanhMuc = TenDanhMuc;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật danh mục thành công!";
            }
            return RedirectToAction("DS_DanhMuc");
        }

        [HttpPost]
        public ActionResult Delete_DanhMuc(string id)
        {
            var dm = db.DanhMucs.Find(id);
            if (dm != null)
            {
                // Kiểm tra xem có sản phẩm nào đang dùng danh mục này không
                if (dm.SanPhams.Any())
                {
                    TempData["Error"] = "Không thể xóa danh mục này vì đang có sản phẩm sử dụng!";
                    return RedirectToAction("DS_DanhMuc");
                }

                db.DanhMucs.Remove(dm);
                db.SaveChanges();
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            return RedirectToAction("DS_DanhMuc");
        }

        // ========== QUẢN LÝ NHÂN VIÊN ==========
        public ActionResult DS_NV(int page = 1, int pageSize = 5)
        {
            ViewBag.Title = "Quản lý Nhân viên";
            
            var query = db.NhanViens;
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            var nhanViens = query
                .OrderBy(nv => nv.MaNV)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            
            return View(nhanViens);
        }

        [HttpPost]
        public ActionResult Create_NV(NhanVien model, string XacNhanMatKhau)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xác nhận mật khẩu
                if (model.MatKhau != XacNhanMatKhau)
                {
                    TempData["Error"] = "Mật khẩu và xác nhận mật khẩu không khớp!";
                    return RedirectToAction("DS_NV");
                }

                // Kiểm tra trùng tên đăng nhập
                if (db.NhanViens.Any(nv => nv.TenDangNhap == model.TenDangNhap))
                {
                    TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                    return RedirectToAction("DS_NV");
                }

                // Tự động tạo mã nhân viên mới (NV001, NV002, ...)
                var lastNhanVien = db.NhanViens.OrderByDescending(nv => nv.MaNV).FirstOrDefault();
                string newMaNV = "NV001";
                
                if (lastNhanVien != null && lastNhanVien.MaNV.StartsWith("NV"))
                {
                    // Lấy số từ mã cuối cùng
                    string numberPart = lastNhanVien.MaNV.Substring(2);
                    if (int.TryParse(numberPart, out int lastNumber))
                    {
                        int newNumber = lastNumber + 1;
                        newMaNV = "NV" + newNumber.ToString("D3"); // D3 để format 001, 002, ...
                    }
                }
                
                model.MaNV = newMaNV;

                db.NhanViens.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Thêm nhân viên thành công!";
            }
            return RedirectToAction("DS_NV");
        }

        [HttpPost]
        public ActionResult Edit_NV(NhanVien model, string XacNhanMatKhau)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xác nhận mật khẩu
                if (model.MatKhau != XacNhanMatKhau)
                {
                    TempData["Error"] = "Mật khẩu và xác nhận mật khẩu không khớp!";
                    return RedirectToAction("DS_NV");
                }

                var nv = db.NhanViens.Find(model.MaNV);
                if (nv != null)
                {
                    // Kiểm tra trùng tên đăng nhập (trừ chính nó)
                    if (db.NhanViens.Any(x => x.TenDangNhap == model.TenDangNhap && x.MaNV != model.MaNV))
                    {
                        TempData["Error"] = "Tên đăng nhập đã tồn tại!";
                        return RedirectToAction("DS_NV");
                    }

                    nv.HoTen = model.HoTen;
                    nv.SoDienThoai = model.SoDienThoai;
                    nv.TenDangNhap = model.TenDangNhap;
                    nv.MatKhau = model.MatKhau;
                    nv.VaiTro = model.VaiTro;

                    db.SaveChanges();
                    TempData["Success"] = "Cập nhật nhân viên thành công!";
                }
            }
            return RedirectToAction("DS_NV");
        }
        public ActionResult TimKiemNV(string search)
        {
            ViewBag.Title = "Tìm kiếm Nhân viên";
            ViewBag.Search = search;
            
            IQueryable<NhanVien> query = db.NhanViens;
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(nv => nv.HoTen.Contains(keyword) || 
                                        nv.TenDangNhap.Contains(keyword) ||
                                        nv.SoDienThoai.Contains(keyword));
            }
            
            var nhanViens = query.OrderBy(nv => nv.HoTen).ToList();
            return View(nhanViens);
        }

        [HttpPost]
        public ActionResult Delete_NV(string id)
        {
            var nv = db.NhanViens.Find(id);
            if (nv != null)
            {
                // Kiểm tra xem nhân viên có đơn hàng không
                if (nv.DonHangs.Any() || nv.DonHangs1.Any())
                {
                    TempData["Error"] = "Không thể xóa nhân viên này vì đang có đơn hàng liên quan!";
                    return RedirectToAction("DS_NV");
                }

                db.NhanViens.Remove(nv);
                db.SaveChanges();
                TempData["Success"] = "Xóa nhân viên thành công!";
            }
            return RedirectToAction("DS_NV");
        }
        // ========== QUẢN LÝ KHÁCH HÀNG ==========
        public ActionResult DS_KH(string search, int page = 1, int pageSize = 5)
        {
            ViewBag.Title = "Quản lý Khách hàng";
            ViewBag.Search = search;
            
            IQueryable<KhachHang> query = db.KhachHangs;
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(kh => kh.HoTen.Contains(keyword) || 
                                        kh.TenDangNhap.Contains(keyword) ||
                                        kh.Email.Contains(keyword) ||
                                        kh.SoDienThoai.Contains(keyword));
            }
            
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            
            var khachHangs = query
                .OrderBy(kh => kh.MaKH)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            
            return View(khachHangs);
        }
        
        public ActionResult Detail_KH(string id)
        {
            ViewBag.Title = "Chi tiết Khách hàng";
            var khachHang = db.KhachHangs.Find(id);
            if (khachHang == null) return HttpNotFound();
            
            // Lấy tất cả đơn hàng của khách hàng, sắp xếp theo ngày đặt mới nhất
            var donHangs = db.DonHangs
                .Include(dh => dh.ChiTietDonHangs)
                .Where(dh => dh.MaKH == id)
                .OrderByDescending(dh => dh.NgayDat)
                .ToList();
            
            ViewBag.DonHangs = donHangs;
            return View(khachHang);
        }
        
        public ActionResult TimKiemKH(string search)
        {
            ViewBag.Title = "Tìm kiếm Khách hàng";
            ViewBag.Search = search;
            
            IQueryable<KhachHang> query = db.KhachHangs;
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(kh => kh.HoTen.Contains(keyword) || 
                                        kh.TenDangNhap.Contains(keyword) ||
                                        kh.Email.Contains(keyword) ||
                                        kh.SoDienThoai.Contains(keyword));
            }
            
            var khachHangs = query.OrderBy(kh => kh.HoTen).ToList();
            return View(khachHangs);
        }
        // ========== THỐNG KÊ DOANH THU ==========
        public ActionResult DoanhThu(int? year, int? month)
        {
            ViewBag.Title = "Thống kê Doanh thu";
            
            int selectedYear = year ?? DateTime.Now.Year;
            int? selectedMonth = month;

            // Lấy dữ liệu doanh thu (chỉ lấy đơn hàng đã hoàn thành)
            var query = db.DonHangs.Where(dh => dh.TrangThai == "HOANTHANH" && dh.DaThanhToan == true);
            
            if (selectedMonth.HasValue)
            {
                query = query.Where(dh => dh.NgayDat.Year == selectedYear && dh.NgayDat.Month == selectedMonth.Value);
            }
            else
            {
                query = query.Where(dh => dh.NgayDat.Year == selectedYear);
            }

            var donHangs = query.Include(dh => dh.ChiTietDonHangs).ToList();
            
            // Tính tổng doanh thu (dùng TongTien từ DonHang nếu có, nếu không thì tính từ ChiTietDonHang)
            decimal tongDoanhThu = 0;
            foreach (var dh in donHangs)
            {
                if (dh.TongTien.HasValue)
                {
                    tongDoanhThu += dh.TongTien.Value;
                }
                else
                {
                    foreach (var ct in dh.ChiTietDonHangs)
                    {
                        tongDoanhThu += ct.SoLuong * ct.DonGia;
                    }
                    if (dh.PhiVanChuyen.HasValue)
                    {
                        tongDoanhThu += dh.PhiVanChuyen.Value;
                    }
                }
            }

            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 6).ToList();
            ViewBag.Months = Enumerable.Range(1, 12).ToList();

            // Dữ liệu cho biểu đồ theo tháng (nếu chọn năm)
            if (!selectedMonth.HasValue)
            {
                var dataByMonth = new List<object>();
                for (int m = 1; m <= 12; m++)
                {
                    var dhThang = donHangs.Where(dh => dh.NgayDat.Month == m).ToList();
                    decimal doanhThuThang = 0;
                    foreach (var dh in dhThang)
                    {
                        if (dh.TongTien.HasValue)
                        {
                            doanhThuThang += dh.TongTien.Value;
                        }
                        else
                        {
                            foreach (var ct in dh.ChiTietDonHangs)
                            {
                                doanhThuThang += ct.SoLuong * ct.DonGia;
                            }
                            if (dh.PhiVanChuyen.HasValue)
                            {
                                doanhThuThang += dh.PhiVanChuyen.Value;
                            }
                        }
                    }
                    dataByMonth.Add(new { month = m, revenue = doanhThuThang });
                }
                ViewBag.ChartData = dataByMonth;
            }
            else
            {
                // Dữ liệu theo ngày trong tháng
                var dataByDay = new List<object>();
                int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth.Value);
                for (int d = 1; d <= daysInMonth; d++)
                {
                    var dhNgay = donHangs.Where(dh => dh.NgayDat.Day == d).ToList();
                    decimal doanhThuNgay = 0;
                    foreach (var dh in dhNgay)
                    {
                        if (dh.TongTien.HasValue)
                        {
                            doanhThuNgay += dh.TongTien.Value;
                        }
                        else
                        {
                            foreach (var ct in dh.ChiTietDonHangs)
                            {
                                doanhThuNgay += ct.SoLuong * ct.DonGia;
                            }
                            if (dh.PhiVanChuyen.HasValue)
                            {
                                doanhThuNgay += dh.PhiVanChuyen.Value;
                            }
                        }
                    }
                    dataByDay.Add(new { day = d, revenue = doanhThuNgay });
                }
                ViewBag.ChartData = dataByDay;
            }

            return View();
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
