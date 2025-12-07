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
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1 db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1();

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

            int maKH = Convert.ToInt32(Session["MaKH"]);
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

            int maKH = Convert.ToInt32(Session["MaKH"]);
            var kh = db.KhachHangs.Find(maKH);

            if (kh == null)
                return HttpNotFound();

            return View(kh);
        }

        // POST: Lưu chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(KhachHang model, string MatKhauCu, string MatKhauMoi, string XacNhanMatKhau)
        {
            if (!ModelState.IsValid)
                return View(model);

            var khachHang = db.KhachHangs.Find(model.MaKH);
            if (khachHang == null)
            {
                ModelState.AddModelError("", "Khách hàng không tồn tại.");
                return View(model);
            }

            // Cập nhật thông tin cơ bản
            khachHang.HoTen = model.HoTen;
            khachHang.SoDienThoai = model.SoDienThoai;
            khachHang.Email = model.Email;

            // Chỉ xử lý đổi mật khẩu nếu người dùng nhập mật khẩu mới
            if (!string.IsNullOrEmpty(MatKhauMoi))
            {
                // Phải nhập mật khẩu cũ
                if (string.IsNullOrEmpty(MatKhauCu))
                {
                    ModelState.AddModelError("", "Vui lòng nhập mật khẩu cũ.");
                    return View(model);
                }

                // Kiểm tra mật khẩu cũ
                if (khachHang.MatKhau != MatKhauCu)
                {
                    ModelState.AddModelError("", "Mật khẩu cũ không đúng.");
                    return View(model);
                }

                // Kiểm tra xác nhận mật khẩu
                if (MatKhauMoi != XacNhanMatKhau)
                {
                    ModelState.AddModelError("XacNhanMatKhau", "Xác nhận mật khẩu mới không khớp.");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                khachHang.MatKhau = MatKhauMoi;
            }

            db.SaveChanges();
            ViewBag.Success = "Cập nhật thông tin thành công!";
            return View(model);
        }

        public ActionResult Menu_KH(string search, int? categoryId, int page = 1, int pageSize = 9)
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
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                int maDanhMuc = categoryId.Value;
                query = query.Where(sp => sp.MaDanhMuc == maDanhMuc);
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