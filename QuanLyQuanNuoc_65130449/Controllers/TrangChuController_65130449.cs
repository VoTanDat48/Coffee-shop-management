using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QuanLyQuanNuoc_65130449.Models;

namespace QuanLyQuanNuoc_65130449.Controllers
{
    public class TrangChuController_65130449Controller : Controller
    {
        private QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1 db =
            new QuanLyQuanNuoc_65130449.Models.QuanLyQuanNuoc_65130449Entities1();

        // GET: TrangChuController_65130449
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

        // Trang Menu: thanh tìm kiếm, phân loại sản phẩm, hiển thị danh sách + phân trang
        public ActionResult Menu(string search, int? categoryId, int page = 1, int pageSize = 9)
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