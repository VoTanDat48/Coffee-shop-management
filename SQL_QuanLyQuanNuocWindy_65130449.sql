CREATE DATABASE QuanLyQuanNuocWindy_65130449
GO
USE QuanLyQuanNuocWindy_65130449
GO

-- Bán Online--

-- 1. Bảng Khách hàng 
CREATE TABLE KhachHang (
    MaKH VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15),
    Email VARCHAR(100) UNIQUE,
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL
);

-- 2. Bảng Nhân viên 
CREATE TABLE NhanVien (
    MaNV VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL UNIQUE,
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(200) NOT NULL,
    VaiTro INT NOT NULL DEFAULT 1 -- 1: NV Duyệt 2: NV giao hàng, 3: Quản lý
);

-- 3. Bảng Danh mục 
CREATE TABLE DanhMuc (
    MaDanhMuc VARCHAR(10) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL UNIQUE
);

-- 4. Bảng Sản Phẩm 
CREATE TABLE SanPham (
    MaSP VARCHAR(10) PRIMARY KEY,
    TenSP NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(10, 2) NOT NULL CHECK (DonGia >= 0),
    MaDanhMuc VARCHAR(10) NOT NULL,
    HinhAnh VARCHAR(255),
    CONSTRAINT FK_SP_DM FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc)
);

-- 5. Bảng GIỎ HÀNG 
CREATE TABLE GioHang (
    MaGioHang VARCHAR(10) PRIMARY KEY,
    MaKH VARCHAR(10) NOT NULL UNIQUE,
    NgayCapNhat DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH)
);

-- 6. Bảng Chi tiết Giỏ hàng
CREATE TABLE ChiTietGioHang (
    MaCTGioHang VARCHAR(10) PRIMARY KEY,
    MaGioHang VARCHAR(10) NOT NULL,
    MaSP VARCHAR(10) NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGiaLucThem DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (MaGioHang) REFERENCES GioHang(MaGioHang),
    FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP)
);

-- 7. Bảng Đơn hàng
CREATE TABLE DonHang (
    MaDonHang VARCHAR(10) PRIMARY KEY,
    MaKH VARCHAR(10) NOT NULL,
    NgayDat DATETIME NOT NULL DEFAULT GETDATE(),
    TrangThai NVARCHAR(20) NOT NULL DEFAULT N'CHODUYET',
    DiaChiGiaoHang NVARCHAR(200) NOT NULL,
    PhiVanChuyen DECIMAL(10,2) DEFAULT 0,
    TongTien DECIMAL(10,2) DEFAULT 0,
    DaThanhToan BIT DEFAULT 0,
    NgayThanhToan DATETIME NULL,
    NhanVienDuyet VARCHAR(10) NULL, -- NV Duyệt (Vai trò 1)
    NhanVienGiao VARCHAR(10) NULL, -- NV Giao hàng (Vai trò 2)
    
    FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    FOREIGN KEY (NhanVienDuyet) REFERENCES NhanVien(MaNV),
    FOREIGN KEY (NhanVienGiao) REFERENCES NhanVien(MaNV)
);

-- 8. Bảng Chi tiết đơn hàng
CREATE TABLE ChiTietDonHang (
    MaCTDH VARCHAR(10) PRIMARY KEY,
    MaDonHang VARCHAR(10) NOT NULL,
    MaSP VARCHAR(10) NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGia DECIMAL(10,2) NOT NULL CHECK (DonGia >= 0),
    FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    FOREIGN KEY (MaSP) REFERENCES SanPham(MaSP)
);
GO

--TRIGGER--
-- Trigger 1: Cập nhật TongTien trong bảng DonHang khi INSERT/UPDATE ChiTietDonHang
CREATE TRIGGER trg_CapNhatTongTien_InsertUpdate_CTDH
ON ChiTietDonHang
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH CTE_MaDonHang AS (
        SELECT MaDonHang FROM inserted GROUP BY MaDonHang
    )
    UPDATE DH
    SET TongTien = ISNULL(
        (
            SELECT SUM(CTDH.SoLuong * CTDH.DonGia) + DH.PhiVanChuyen
            FROM ChiTietDonHang CTDH
            WHERE CTDH.MaDonHang = DH.MaDonHang
        ), 0)
    FROM DonHang DH
    INNER JOIN CTE_MaDonHang CTE ON DH.MaDonHang = CTE.MaDonHang;
END;
GO

-- Trigger 2: Cập nhật TongTien trong bảng DonHang khi DELETE ChiTietDonHang
CREATE TRIGGER trg_CapNhatTongTien_Delete_CTDH
ON ChiTietDonHang
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH CTE_MaDonHang AS (
        SELECT MaDonHang FROM deleted
    )
    UPDATE DH
    SET TongTien = ISNULL(
        (
            SELECT SUM(CTDH.SoLuong * CTDH.DonGia) + DH.PhiVanChuyen
            FROM ChiTietDonHang CTDH
            WHERE CTDH.MaDonHang = DH.MaDonHang
        ), DH.PhiVanChuyen)
    FROM DonHang DH
    INNER JOIN CTE_MaDonHang CTE ON DH.MaDonHang = CTE.MaDonHang;
END;
GO

-- Trigger 3: Cập nhật NgayThanhToan khi DaThanhToan chuyển sang 1
CREATE TRIGGER trg_CapNhatThoiGianThanhToan
ON DonHang
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE DH
    SET NgayThanhToan = GETDATE()
    FROM DonHang DH
    INNER JOIN inserted i ON DH.MaDonHang = i.MaDonHang
    INNER JOIN deleted d ON DH.MaDonHang = d.MaDonHang
    WHERE i.DaThanhToan = 1 
      AND d.DaThanhToan = 0
      AND DH.NgayThanhToan IS NULL;
END;
GO

-- Xóa bảng có khóa ngoại trước
DROP TABLE IF EXISTS HoaDon
DROP TABLE IF EXISTS ChiTietDonHang
DROP TABLE IF EXISTS DonHang
DROP TABLE IF EXISTS SanPham
DROP TABLE IF EXISTS NhanVien
DROP TABLE IF EXISTS KhachHang
DROP TABLE IF EXISTS DanhMuc
GO

--Chèn dữ liệu--
-- 1. Khách hàng (30)
INSERT INTO KhachHang (MaKH, HoTen, SoDienThoai, Email, TenDangNhap, MatKhau)
VALUES
    ('KH0001', N'Võ Danh', '0912345678', 'danh@example.com', 'danh_user', '123456'),
    ('KH0002', N'Công Sơn', '0988776655', 'son@example.com', 'son_user', '123456'),
    ('KH0003', N'Nguyễn Văn Đại', '0901112233', 'dai@example.com', 'dai_user', 'abc123'),
    ('KH0004', N'Trần Thị Mai', '0910000004', 'mai.t@example.com', 'mai_t', 'pass123'),
    ('KH0005', N'Lê Văn Hùng', '0910000005', 'hung.l@example.com', 'hung_l', 'pass123'),
    ('KH0006', N'Phạm Thu Thảo', '0910000006', 'thao.p@example.com', 'thao_p', 'pass123'),
    ('KH0007', N'Hoàng Đình Kiên', '0910000007', 'kien.h@example.com', 'kien.h', 'pass123'),
    ('KH0008', N'Đỗ Minh Châu', '0910000008', 'chau.d@example.com', 'chau.d', 'pass123'),
    ('KH0009', N'Vũ Quốc Việt', '0910000009', 'viet.v@example.com', 'viet.v', 'pass123'),
    ('KH0010', N'Nguyễn Thị Kim', '0910000010', 'kim.n@example.com', 'kim.n', 'pass123'),
    ('KH0011', N'Trịnh Văn An', '0910000011', 'an.t@example.com', 'an.t', 'pass123'),
    ('KH0012', N'Bùi Thị Hồng', '0910000012', 'hong.b@example.com', 'hong.b', 'pass123'),
    ('KH0013', N'Đào Công Hậu', '0910000013', 'hau.d@example.com', 'hau.d', 'pass123'),
    ('KH0014', 'Lý Mỹ Duyên', '0910000014', 'duyen.l@example.com', 'duyen.l', 'pass123'),
    ('KH0015', N'Tô Văn Thanh', '0910000015', 'thanh.t@example.com', 'thanh.t', 'pass123'),
    ('KH0016', N'Cao Xuân Phát', '0910000016', 'phat.c@example.com', 'phat.c', 'pass123'),
    ('KH0017', N'Dương Thị Lan', '0910000017', 'lan.d@example.com', 'lan.d', 'pass123'),
    ('KH0018', N'Huỳnh Đức Lộc', '0910000018', 'loc.h@example.com', 'loc.h', 'pass123'),
    ('KH0019', N'Kiều Minh Tuấn', '0910000019', 'tuan.k@example.com', 'tuan.k', 'pass123'),
    ('KH0020', N'Mạc Ngân Hà', '0910000020', 'ha.m@example.com', 'ha.m', 'pass123'),
    ('KH0021', N'Ngô Gia Bảo', '0910000021', 'bao.n@example.com', 'bao.n', 'pass123'),
    ('KH0022', N'Phan Lương Bằng', '0910000022', 'bang.p@example.com', 'bang.p', 'pass123'),
    ('KH0023', N'Quách Ngọc Tuyên', '0910000023', 'tuyen.q@example.com', 'tuyen.q', 'pass123'),
    ('KH0024', N'Tăng Thanh Hà', '0910000024', 'ha.t@example.com', 'ha.t', 'pass123'),
    ('KH0025', N'Thái Hòa', '0910000025', 'hoa.t@example.com', 'hoa.t', 'pass123'),
    ('KH0026', N'Võ Hoàng Yến', '0910000026', 'yen.v@example.com', 'yen.v', 'pass123'),
    ('KH0027', N'Xuyến Chi', '0910000027', 'chi.x@example.com', 'chi.x', 'pass123'),
    ('KH0028', N'Yến Nhi', '0910000028', 'nhi.y@example.com', 'nhi.y', 'pass123'),
    ('KH0029', N'Zoe Nguyễn', '0910000029', 'zoe.n@example.com', 'zoe.n', 'pass123'),
    ('KH0030', N'Ân Đức', '0910000030', 'an.d@example.com', 'an.d', 'pass123');

-- 2. Nhân viên (3 người: NV001: Quản lý, NV002: Duyệt, NV003: Giao hàng)
INSERT INTO NhanVien (MaNV, HoTen, SoDienThoai,TenDangNhap, MatKhau, VaiTro)
VALUES
    ('NV001', N'Võ Tấn Đạt', '0900000001', 'admin_a', 'admin123', 3), -- Quản lý
    ('NV002', N'Võ Công Thành', '0900000002', 'duyet_1', 'nv123', 1), -- Duyệt đơn
    ('NV003', N'Nguyễn Quốc Thắng', '0900000003', 'giaohang_1', 'nv123', 2); -- Giao hàng

-- 3. Danh mục (4)
INSERT INTO DanhMuc (MaDanhMuc, TenDanhMuc)
VALUES  
    ('DM01', N'Trà Sữa'),  
    ('DM02', N'Cà Phê'),  
    ('DM03', N'Sinh Tố'),  
    ('DM04', N'Nước Ngọt');

-- 4. Sản phẩm (16)
INSERT INTO SanPham (MaSP, TenSP, DonGia, MaDanhMuc)
VALUES  
    ('SP001', N'Trà Sữa Truyền Thống', 30000.00, 'DM01'),
    ('SP002', N'Trà Sữa Matcha', 35000.00, 'DM01'),
    ('SP003', N'Trà Sữa Socola', 35000.00, 'DM01'),
    ('SP004', N'Trà Sữa Khoai Môn', 35000.00, 'DM01'),
    ('SP005', N'Cà Phê Đen Đá', 20000.00, 'DM02'),
    ('SP006', N'Cà Phê Sữa Đá', 25000.00, 'DM02'),
    ('SP007', N'Bạc Xỉu', 25000.00, 'DM02'),
    ('SP008', N'Capuchino', 35000.00, 'DM02'),
    ('SP009', N'Sinh Tố Bơ', 25000.00, 'DM03'),
    ('SP010', N'Sinh Tố Xoài', 25000.00, 'DM03'),
    ('SP011', N'Sinh Tố Đu Đủ', 25000.00, 'DM03'),
    ('SP012', N'Sinh Tố Dưa Hấu', 25000.00, 'DM03'),
    ('SP013', N'Coca Cola', 15000.00, 'DM04'),
    ('SP014', N'Pepsi', 15000.00, 'DM04'),
    ('SP015', N'7Up', 15000.00, 'DM04'),
    ('SP016', N'Nước Suối', 10000.00, 'DM04');

-- 6. Đơn hàng (30) - NV Duyệt là NV002, NV Giao là NV003
INSERT INTO DonHang (MaDonHang, MaKH, NgayDat, TrangThai, DiaChiGiaoHang, PhiVanChuyen, NhanVienDuyet, NhanVienGiao, DaThanhToan)
VALUES
    ('DH0001', 'KH0001', GETDATE(), N'CHODUYET', N'123 Đường A, Quận 1', 15000, 'NV002', NULL, 0), -- Chờ duyệt
    ('DH0002', 'KH0002', GETDATE(), N'DANGPHACHE', N'456 Đường B, Quận 3', 20000, 'NV002', NULL, 0), -- Đang pha chế
    ('DH0003', 'KH0003', GETDATE(), N'HOANTHANH', N'789 Đường C, Quận 10', 20000, 'NV002', 'NV003', 1), -- Hoàn thành

    -- Giao dịch 2024 - Hoàn thành & Đã thanh toán (Sử dụng NV002/NV003 cho tất cả)
    ('DH0004', 'KH0004', '2024-03-15 10:00:00', N'HOANTHANH', N'101 Phố D', 15000, 'NV002', 'NV003', 1),
    ('DH0005', 'KH0005', '2024-03-15 11:30:00', N'HOANTHANH', N'102 Phố E', 15000, 'NV002', 'NV003', 1),
    ('DH0006', 'KH0006', '2024-04-01 14:00:00', N'HOANTHANH', N'103 Phố F', 25000, 'NV002', 'NV003', 1),
    ('DH0007', 'KH0007', '2024-04-05 09:45:00', N'HOANTHANH', N'104 Phố G', 20000, 'NV002', 'NV003', 1),
    ('DH0008', 'KH0008', '2024-05-20 16:30:00', N'HOANTHANH', N'105 Phố H', 15000, 'NV002', 'NV003', 1),
    ('DH0009', 'KH0009', '2024-06-10 12:00:00', N'HOANTHANH', N'106 Phố I', 15000, 'NV002', 'NV003', 1),
    ('DH0010', 'KH0010', '2024-07-22 17:15:00', N'HOANTHANH', N'107 Phố J', 20000, 'NV002', 'NV003', 1),
    ('DH0011', 'KH0011', '2024-08-01 10:20:00', N'HOANTHANH', N'108 Phố K', 25000, 'NV002', 'NV003', 1),
    ('DH0012', 'KH0012', '2024-09-09 15:40:00', N'HOANTHANH', N'109 Phố L', 15000, 'NV002', 'NV003', 1),
    ('DH0013', 'KH0013', '2024-10-18 11:00:00', N'HOANTHANH', N'110 Phố M', 20000, 'NV002', 'NV003', 1),

    -- Giao dịch 11/2025 - Hoàn thành & Đã thanh toán
    ('DH0014', 'KH0014', '2025-11-01 08:30:00', N'HOANTHANH', N'201 Đường N', 15000, 'NV002', 'NV003', 1),
    ('DH0015', 'KH0015', '2025-11-05 13:10:00', N'HOANTHANH', N'202 Đường O', 15000, 'NV002', 'NV003', 1),
    ('DH0016', 'KH0016', '2025-11-08 17:00:00', N'HOANTHANH', N'203 Đường P', 20000, 'NV002', 'NV003', 1),
    ('DH0017', 'KH0017', '2025-11-10 10:45:00', N'HOANTHANH', N'204 Đường Q', 20000, 'NV002', 'NV003', 1),
    ('DH0018', 'KH0018', '2025-11-12 11:00:00', N'HOANTHANH', N'205 Đường R', 15000, 'NV002', 'NV003', 1),
    ('DH0019', 'KH0019', '2025-11-15 14:20:00', N'HOANTHANH', N'206 Đường S', 15000, 'NV002', 'NV003', 1),
    ('DH0020', 'KH0020', '2025-11-18 09:30:00', N'HOANTHANH', N'207 Đường T', 20000, 'NV002', 'NV003', 1),
    ('DH0021', 'KH0021', '2025-11-20 16:50:00', N'HOANTHANH', N'208 Đường U', 25000, 'NV002', 'NV003', 1),
    ('DH0022', 'KH0022', '2025-11-25 12:00:00', N'HOANTHANH', N'209 Đường V', 15000, 'NV002', 'NV003', 1),
    ('DH0023', 'KH0023', '2025-11-28 15:15:00', N'HOANTHANH', N'210 Đường W', 20000, 'NV002', 'NV003', 1),

    -- Giao dịch hiện tại (Đang xử lý)
    ('DH0024', 'KH0024', GETDATE(), N'CHODUYET', N'301 Đường X', 15000, 'NV002', NULL, 0),
    ('DH0025', 'KH0025', DATEADD(HOUR, -2, GETDATE()), N'DANGGIAO', N'302 Đường Y', 20000, 'NV002', 'NV003', 0), -- Đang giao
    ('DH0026', 'KH0026', DATEADD(HOUR, -3, GETDATE()), N'HOANTHANH', N'303 Đường Z', 15000, 'NV002', 'NV003', 1),
    ('DH0027', 'KH0027', DATEADD(HOUR, -5, GETDATE()), N'DANGPHACHE', N'304 Đường A1', 25000, 'NV002', NULL, 0),
    ('DH0028', 'KH0028', DATEADD(HOUR, -8, GETDATE()), N'CHODUYET', N'305 Đường B1', 15000, 'NV002', NULL, 0),
    ('DH0029', 'KH0029', DATEADD(DAY, -1, GETDATE()), N'DANGGIAO', N'306 Đường C1', 20000, 'NV002', 'NV003', 0),
    ('DH0030', 'KH0030', DATEADD(DAY, -2, GETDATE()), N'HOANTHANH', N'307 Đường D1', 15000, 'NV002', 'NV003', 1);

-- Đơn hàng 1 (DH0001)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH001', 'DH0001', 'SP001', 1, 30000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH002', 'DH0001', 'SP004', 2, 35000.00); 

-- Đơn hàng 2 (DH0002)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH003', 'DH0002', 'SP005', 1, 20000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH004', 'DH0002', 'SP007', 2, 25000.00); 

-- Đơn hàng 3 (DH0003)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH005', 'DH0003', 'SP013', 1, 15000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH006', 'DH0003', 'SP016', 1, 10000.00); 

-- Đơn hàng 4 (DH0004)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH007', 'DH0004', 'SP009', 2, 25000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH008', 'DH0004', 'SP010', 1, 25000.00); 

-- Đơn hàng 5 (DH0005)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH009', 'DH0005', 'SP002', 1, 35000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH010', 'DH0005', 'SP006', 2, 25000.00);

-- Đơn hàng 6 (DH0006)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH011', 'DH0006', 'SP011', 3, 25000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH012', 'DH0006', 'SP014', 1, 15000.00); 

-- Đơn hàng 7 (DH0007)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH013', 'DH0007', 'SP003', 1, 35000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH014', 'DH0007', 'SP008', 1, 35000.00); 

-- Đơn hàng 8 (DH0008)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH015', 'DH0008', 'SP001', 2, 30000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH016', 'DH0008', 'SP005', 1, 20000.00); 

-- Đơn hàng 9 (DH0009)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH017', 'DH0009', 'SP001', 1, 30000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH018', 'DH0009', 'SP015', 2, 15000.00); 

-- Đơn hàng 10 (DH0010)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH019', 'DH0010', 'SP009', 1, 25000.00); 
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH020', 'DH0010', 'SP013', 2, 15000.00); 

-- Đơn hàng 11 (DH0011)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH021', 'DH0011', 'SP006', 1, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH022', 'DH0011', 'SP012', 3, 25000.00);

-- Đơn hàng 12 (DH0012)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH023', 'DH0012', 'SP005', 2, 20000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH024', 'DH0012', 'SP016', 1, 10000.00);

-- Đơn hàng 13 (DH0013)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH025', 'DH0013', 'SP007', 1, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH026', 'DH0013', 'SP013', 2, 15000.00);

-- Đơn hàng 14 (DH0014)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH027', 'DH0014', 'SP001', 3, 30000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH028', 'DH0014', 'SP008', 1, 35000.00);

-- Đơn hàng 15 (DH0015)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH029', 'DH0015', 'SP004', 1, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH030', 'DH0015', 'SP011', 2, 25000.00);

-- Đơn hàng 16 (DH0016)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH031', 'DH0016', 'SP006', 2, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH032', 'DH0016', 'SP009', 1, 25000.00);

-- Đơn hàng 17 (DH0017)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH033', 'DH0017', 'SP002', 1, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH034', 'DH0017', 'SP014', 2, 15000.00);

-- Đơn hàng 18 (DH0018)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH035', 'DH0018', 'SP008', 3, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH036', 'DH0018', 'SP013', 1, 15000.00);

-- Đơn hàng 19 (DH0019)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH037', 'DH0019', 'SP002', 2, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH038', 'DH0019', 'SP014', 1, 15000.00);

-- Đơn hàng 20 (DH0020)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH039', 'DH0020', 'SP010', 1, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH040', 'DH0020', 'SP015', 1, 15000.00);

-- Đơn hàng 21 (DH0021)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH041', 'DH0021', 'SP003', 2, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH042', 'DH0021', 'SP011', 1, 25000.00);

-- Đơn hàng 22 (DH0022)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH043', 'DH0022', 'SP005', 1, 20000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH044', 'DH0022', 'SP012', 2, 25000.00);

-- Đơn hàng 23 (DH0023)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH045', 'DH0023', 'SP007', 3, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH046', 'DH0023', 'SP016', 1, 10000.00);

-- Đơn hàng 24 (DH0024) - Chờ duyệt
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH047', 'DH0024', 'SP001', 1, 30000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH048', 'DH0024', 'SP005', 1, 20000.00);

-- Đơn hàng 25 (DH0025) - Đang giao
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH049', 'DH0025', 'SP006', 2, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH050', 'DH0025', 'SP003', 1, 35000.00);

-- Đơn hàng 26 (DH0026) - Hoàn thành (Gần đây)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH051', 'DH0026', 'SP004', 1, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH052', 'DH0026', 'SP008', 2, 35000.00);

-- Đơn hàng 27 (DH0027) - Đang pha chế
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH053', 'DH0027', 'SP005', 3, 20000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH054', 'DH0027', 'SP009', 1, 25000.00);

-- Đơn hàng 28 (DH0028) - Chờ duyệt
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH055', 'DH0028', 'SP010', 1, 25000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH056', 'DH0028', 'SP013', 2, 15000.00);

-- Đơn hàng 29 (DH0029) - Đang giao
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH057', 'DH0029', 'SP002', 1, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH058', 'DH0029', 'SP014', 1, 15000.00);

-- Đơn hàng 30 (DH0030) - Hoàn thành (Gần đây)
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH059', 'DH0030', 'SP003', 2, 35000.00);
INSERT INTO ChiTietDonHang (MaCTDH, MaDonHang, MaSP, SoLuong, DonGia) VALUES ('CTDH060', 'DH0030', 'SP007', 1, 25000.00);
GO

-- Cập nhật NgayThanhToan cho các đơn hàng đã thanh toán (DaThanhToan = 1)
UPDATE DonHang
SET DaThanhToan = 1, NgayThanhToan = NgayDat
WHERE DaThanhToan = 1;

-- Cập nhật TongTien lần đầu
UPDATE DonHang
SET TongTien = ISNULL(
    (
        SELECT SUM(CTDH.SoLuong * CTDH.DonGia)
        FROM ChiTietDonHang CTDH
        WHERE CTDH.MaDonHang = DonHang.MaDonHang
    ), 0) + DonHang.PhiVanChuyen;
GO
