# FEEDBACK DỰ ÁN QUẢN LÝ QUÁN NƯỚC

## TỔNG QUAN
Dự án được xây dựng trên nền tảng ASP.NET MVC 5 với Entity Framework Database First, sử dụng SQL Server. Hệ thống quản lý quán nước với các chức năng cơ bản: quản lý sản phẩm, đơn hàng, khách hàng, nhân viên và thống kê doanh thu.

---

## ⚠️ CÁC VẤN ĐỀ BẢO MẬT NGHIÊM TRỌNG

### 1. Mật khẩu lưu dạng Plain Text (CỰC KỲ NGUY HIỂM)
**Vị trí:** Tất cả các bảng KhachHang, NhanVien

**Vấn đề:**
- Mật khẩu được lưu trực tiếp vào database mà không có bất kỳ hình thức mã hóa nào
- Trong AccountController, mật khẩu được so sánh trực tiếp: `nv.MatKhau == MatKhau`

**Hậu quả:**
- Nếu database bị rò rỉ, tất cả mật khẩu người dùng đều bị lộ
- Vi phạm nghiêm trọng các nguyên tắc bảo mật cơ bản
- Không tuân thủ các tiêu chuẩn bảo mật (OWASP, PCI-DSS)

**Giải pháp:**
```csharp
// Sử dụng BCrypt hoặc ASP.NET Identity Password Hasher
using BCrypt.Net;

// Khi đăng ký/đổi mật khẩu
string hashedPassword = BCrypt.HashPassword(plainPassword);

// Khi đăng nhập
bool isValid = BCrypt.Verify(plainPassword, hashedPassword);
```

### 2. Thông tin nhạy cảm hardcoded trong Web.config
**Vị trí:** `Web.config` dòng 71-77

**Vấn đề:**
- Connection string chứa username và password SQL Server: `user id=sa;password=482005`
- Trong AccountController dòng 174: Gmail password hardcoded: `string fromPassword = "htap ygmb vflq orio";`

**Hậu quả:**
- Thông tin đăng nhập database dễ bị lộ nếu source code bị public
- Gmail password có thể bị đánh cắp

**Giải pháp:**
- Sử dụng User Secrets cho development
- Sử dụng Azure Key Vault hoặc environment variables cho production
- Không commit Web.config có thông tin nhạy cảm vào Git

### 3. Session-based Authorization yếu
**Vị trí:** `AdminAuthorize.cs`, `AccountController.cs`

**Vấn đề:**
- Chỉ dựa vào Session["UserRole"] để kiểm tra quyền
- Session có thể bị giả mạo hoặc tamper
- Không có mechanism để kiểm tra session timeout đúng cách

**Giải pháp:**
- Sử dụng FormsAuthentication với encrypted ticket
- Implement proper session timeout
- Thêm token validation

---

## 📝 CÁC VẤN ĐỀ VỀ CODE QUALITY

### 1. Đặt tên không tuân thủ Convention
**Vấn đề:**
- Tên Controller có suffix `_65130449`: `AccountController_65130449Controller`, `AdminController_65130449Controller`
- Tên namespace/file không nhất quán với tên class

**Ví dụ:**
```csharp
// ❌ Không đúng convention
public class AccountController_65130449Controller : Controller
public class AdminController_65130449Controller : Controller

// ✅ Nên là
public class AccountController : Controller
public class AdminController : Controller
```

**Giải pháp:**
- Loại bỏ suffix `_65130449` khỏi tên class
- Nếu cần phân biệt, sử dụng namespace hoặc project name

### 2. Controller quá dài, vi phạm Single Responsibility Principle
**Vị trí:** `AdminController_65130449.cs` (636 dòng), `KhachHangController_65130449.cs` (727 dòng)

**Vấn đề:**
- Một controller xử lý quá nhiều chức năng
- Khó maintain và test
- Logic business trộn lẫn với logic presentation

**Giải pháp:**
- Tách thành các controller nhỏ hơn: `ProductController`, `CategoryController`, `EmployeeController`, `CustomerController`
- Tạo Service Layer để chứa business logic
- Sử dụng Repository Pattern để tách biệt data access

### 3. Code trùng lặp (DRY Violation)

#### a) Logic tạo mã tự động lặp lại nhiều nơi
**Vị trí:** 
- `AdminController.cs`: Create_SP, Create_DanhMuc, Create_NV
- `AccountController.cs`: Register
- `KhachHangController.cs`: GenerateNextId (có nhưng vẫn có chỗ dùng logic cũ)

**Vấn đề:**
- Mỗi nơi đều tự implement logic tạo mã
- Khó maintain, dễ sai sót

**Giải pháp:**
```csharp
// Tạo một service chung
public class IdGeneratorService
{
    public string GenerateNextId<T>(string prefix, DbSet<T> dbSet, Func<T, string> getIdFunc) where T : class
    {
        // Logic chung
    }
}
```

#### b) Logic tính tổng tiền lặp lại
**Vị trí:** `AdminController.DoanhThu()` - nhiều đoạn code tính tổng tiền giống nhau

**Giải pháp:** Tạo helper method hoặc extension method

### 4. Magic Numbers và Hardcoded Values
**Vấn đề:**
- `phiVanChuyen = 15000` hardcoded ở nhiều nơi
- `VaiTro` dùng số (1, 2, 3) thay vì enum hoặc constant
- `pageSize = 5` hoặc `pageSize = 9` không được config

**Giải pháp:**
```csharp
// Sử dụng constants
public static class Constants
{
    public const decimal DEFAULT_SHIPPING_FEE = 15000;
    public const int DEFAULT_PAGE_SIZE = 10;
}

// Hoặc dùng enum cho VaiTro
public enum VaiTroNhanVien
{
    NhanVienDuyet = 1,
    NhanVienGiaoHang = 2,
    QuanLy = 3
}
```

### 5. Thiếu Validation đầy đủ

#### a) Validation input
- Một số chỗ có validation tốt (EditAccount), nhưng chỗ khác thiếu
- Không validate file upload size, type đầy đủ
- Thiếu validation cho số lượng sản phẩm (có thể âm?)

#### b) Validation business logic
- Không kiểm tra số lượng sản phẩm còn tồn kho khi đặt hàng
- Không kiểm tra giá sản phẩm có thay đổi sau khi thêm vào giỏ hàng
- `TongTien` trong Checkout được set = 0 nhưng không được tính lại (có thể dựa vào trigger DB, nhưng nên tính trong code)

### 6. Error Handling không nhất quán
**Vấn đề:**
- Một số nơi có try-catch tốt, một số nơi không có
- Có chỗ catch nhưng chỉ log ra console (dòng 227 trong KhachHangController)
- Thông báo lỗi không user-friendly ở một số chỗ

**Ví dụ:**
```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Exception: {ex.Message}"); // ❌ Chỉ log ra debug
    // Nên log vào file hoặc logging system
}
```

**Giải pháp:**
- Sử dụng logging framework (NLog, Serilog)
- Implement global error handler
- Tạo custom exception classes

### 7. SQL Injection Risk (Mặc dù dùng EF)
**Vấn đề:**
- Đang dùng Entity Framework nên phần lớn an toàn
- Nhưng cần lưu ý khi dùng raw SQL queries (nếu có)

### 8. Thiếu Transaction Management
**Vị trí:** `KhachHangController.Checkout()`

**Vấn đề:**
- Checkout tạo đơn hàng, chi tiết đơn hàng, xóa giỏ hàng - nhiều operations nhưng không có transaction
- Nếu một bước fail, data có thể inconsistent

**Giải pháp:**
```csharp
using (var transaction = db.Database.BeginTransaction())
{
    try
    {
        // All operations
        db.SaveChanges();
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

---

## 🏗️ KIẾN TRÚC VÀ THIẾT KẾ

### 1. Thiếu Service Layer
**Vấn đề:**
- Business logic nằm trực tiếp trong Controller
- Khó test và reuse code
- Vi phạm Separation of Concerns

**Giải pháp:**
- Tạo các Service classes: `ProductService`, `OrderService`, `CustomerService`
- Controller chỉ nên gọi service và trả về view

### 2. Thiếu Repository Pattern
**Vị trí:** Tất cả controllers đều tạo DbContext trực tiếp

**Vấn đề:**
- Khó mock cho unit test
- Data access logic rải rác trong controller

**Giải pháp:**
```csharp
public interface IProductRepository
{
    IEnumerable<SanPham> GetAll();
    SanPham GetById(string id);
    void Add(SanPham product);
    // ...
}
```

### 3. DbContext không được quản lý tốt
**Vấn đề:**
- Mỗi controller tạo một instance DbContext mới
- Có Dispose nhưng không dùng Dependency Injection
- Khó quản lý lifecycle

**Giải pháp:**
- Sử dụng Dependency Injection (Unity, Autofac, hoặc built-in DI của .NET Core)
- Configure DbContext lifetime trong DI container

### 4. Thiếu DTOs/ViewModels
**Vấn đề:**
- Truyền trực tiếp Entity models vào View
- Không có layer để transform data
- Expose quá nhiều thông tin không cần thiết

**Giải pháp:**
- Tạo ViewModels cho mỗi View
- Tạo DTOs cho API responses (nếu có API)

---

## 🔍 CHẤT LƯỢNG CODE CỤ THỂ

### 1. AccountController - Login Logic
**Vấn đề:**
- Dòng 44: Luôn set `Session["UserRole"] = "Admin"` cho tất cả nhân viên, không phân biệt VaiTro
- Redirect không đúng: Dòng 53, 55, 57 redirect đến controller không tồn tại (`Employee`)
- Dòng 79: Redirect đến `KhachHangController_65130449` nhưng tên controller thực tế có thể khác

**Giải pháp:**
```csharp
// Set role đúng
Session["UserRole"] = nhanVien.VaiTro == 3 ? "Admin" : "Employee";
// Hoặc dùng enum/constant thay vì magic number
```

### 2. AdminController - DoanhThu
**Vấn đề:**
- Code tính tổng tiền lặp lại 3 lần (dòng 536-553, 568-586, 599-617)
- Logic phức tạp, khó đọc

**Giải pháp:** Tách thành method riêng

### 3. KhachHangController - Checkout
**Vấn đề:**
- Dòng 595: `TongTien = 0` - không tính tổng tiền (có thể dựa vào trigger DB)
- Logic tạo mã CTDH phức tạp (dòng 603-628)
- Thiếu transaction như đã đề cập

### 4. Code Comment không đầy đủ
**Vấn đề:**
- Một số đoạn code phức tạp không có comment
- Comment bằng tiếng Việt (OK cho project nội bộ, nhưng nên consistent)

---

## 🎨 UI/UX (Đánh giá qua cấu trúc View)

### Điểm tốt:
- Có phân chia Layout cho từng role (Admin, KhachHang, TrangChu)
- Có sử dụng Bootstrap (dựa vào packages)

### Cần cải thiện:
- Không thể đánh giá chi tiết UI mà không xem code View, nhưng nên đảm bảo:
  - Responsive design
  - Error messages hiển thị rõ ràng
  - Loading states khi submit form
  - Confirmation dialogs cho các thao tác quan trọng (xóa, đặt hàng)

---

## 📊 DATABASE DESIGN

### Điểm tốt:
- Có sử dụng Foreign Keys
- Có Triggers để tính TongTien tự động
- Schema khá rõ ràng

### Cần cải thiện:
- Mật khẩu nên lưu hashed (đã đề cập ở phần bảo mật)
- Nên có bảng lưu lịch sử thay đổi giá sản phẩm
- Nên có bảng Audit log cho các thao tác quan trọng
- VaiTro nên dùng lookup table thay vì hardcode số

---

## ✅ ĐIỂM TÍCH CỰC

1. **Cấu trúc dự án rõ ràng:** Tuân thủ MVC pattern
2. **Có validation:** Một số chỗ validation khá tốt (Email, Số điện thoại)
3. **Có phân trang:** Implement phân trang cho danh sách
4. **Có tìm kiếm và lọc:** Menu có chức năng tìm kiếm và lọc theo danh mục
5. **Error handling:** Có cố gắng xử lý lỗi ở một số chỗ
6. **Authorization:** Có implement AdminAuthorize attribute
7. **Giỏ hàng:** Logic giỏ hàng được implement đầy đủ
8. **Checkout flow:** Có đầy đủ các bước đặt hàng

---

## 🎯 KHUYẾN NGHỊ ƯU TIÊN

### Priority 1 - Cực kỳ quan trọng (Phải sửa ngay):
1. ✅ Hash mật khẩu trước khi lưu vào database
2. ✅ Di chuyển thông tin nhạy cảm ra khỏi Web.config
3. ✅ Fix logic phân quyền trong Login (không phải tất cả nhân viên đều là Admin)

### Priority 2 - Quan trọng (Nên sửa sớm):
4. ✅ Refactor tên Controller (bỏ suffix _65130449)
5. ✅ Tách Controller lớn thành các controller nhỏ hơn
6. ✅ Implement Transaction cho Checkout
7. ✅ Tạo Service Layer để tách business logic
8. ✅ Implement logging system thay vì Debug.WriteLine

### Priority 3 - Nên cải thiện (Có thể làm sau):
9. ✅ Implement Repository Pattern
10. ✅ Tạo ViewModels thay vì dùng trực tiếp Entity
11. ✅ Refactor code trùng lặp (IdGenerator, tính tổng tiền)
12. ✅ Sử dụng Dependency Injection
13. ✅ Tạo constants/enum cho magic numbers
14. ✅ Cải thiện error handling và user messages

---

## 📚 TÀI LIỆU THAM KHẢO

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET MVC Best Practices](https://docs.microsoft.com/en-us/aspnet/mvc/overview/getting-started/introduction/getting-started)
- [Entity Framework Best Practices](https://docs.microsoft.com/en-us/ef/core/performance/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)

---

## KẾT LUẬN

Dự án có cấu trúc cơ bản tốt và các chức năng chính đã được implement. Tuy nhiên, có một số vấn đề bảo mật nghiêm trọng cần được xử lý ngay lập tức, đặc biệt là việc lưu mật khẩu dạng plain text. Về mặt code quality, dự án cần được refactor để dễ maintain và mở rộng hơn trong tương lai.

**Điểm đánh giá tổng thể: 6.5/10**
- Functionality: 7/10
- Security: 3/10 (do vấn đề mật khẩu)
- Code Quality: 6/10
- Architecture: 6/10
- Best Practices: 5/10

---

*Feedback được tạo dựa trên phân tích toàn bộ codebase dự án.*

