-- ===================================================
-- SQL thao tac nhanh cho CarProject (BAN AN TOAN, IDEMPOTENT)
-- Chay trong SSMS (chon database CarShopDb truoc)
-- KHONG XOA bat ky du lieu nao. Toan bo la UPSERT, chay lai nhieu lan OK.
-- GIU NGUYEN anh tren server: DuongDanLogo (HangXe), DuongDanAnh (DongXe/PhienBanXe/QuangCaoBanner), HinhAnhXe
-- Ton kho dong bo theo dung du lieu hien tai cua DB local (sinh ngay 2026-08-04)
-- ===================================================

-- ==================== 0. CẬP NHẬT SCHEMA ====================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DonDatCoc') AND name = 'MaChiNhanh')
    ALTER TABLE DonDatCoc ADD MaChiNhanh NVARCHAR(450) NULL;
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DonDatCoc') AND name = 'MaChiNhanh' AND max_length = 40)
    ALTER TABLE DonDatCoc ALTER COLUMN MaChiNhanh NVARCHAR(450) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HoaDonMuaXe') AND name = 'MaChiNhanh')
    ALTER TABLE HoaDonMuaXe ADD MaChiNhanh NVARCHAR(450) NULL;
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HoaDonMuaXe') AND name = 'MaChiNhanh' AND max_length = 40)
    ALTER TABLE HoaDonMuaXe ALTER COLUMN MaChiNhanh NVARCHAR(450) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DongXe') AND name = 'NoiBat')
    ALTER TABLE DongXe ADD NoiBat BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DonDatCoc_ChiNhanhShowroom')
    ALTER TABLE DonDatCoc ADD CONSTRAINT FK_DonDatCoc_ChiNhanhShowroom FOREIGN KEY (MaChiNhanh) REFERENCES ChiNhanhShowroom(MaChiNhanh);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_HoaDonMuaXe_ChiNhanhShowroom')
    ALTER TABLE HoaDonMuaXe ADD CONSTRAINT FK_HoaDonMuaXe_ChiNhanhShowroom FOREIGN KEY (MaChiNhanh) REFERENCES ChiNhanhShowroom(MaChiNhanh);
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DonDatCocChiTiet')
BEGIN
    CREATE TABLE DonDatCocChiTiet (
        MaChiTiet INT IDENTITY(1,1) NOT NULL,
        MaDonCoc INT NOT NULL,
        MaPhienBan INT NOT NULL,
        MaChiNhanh NVARCHAR(450) NULL,
        SoLuong INT NOT NULL,
        TrangThaiTiepNhan NVARCHAR(MAX) NOT NULL DEFAULT N'Chờ xác nhận',
        LyDoTuChoi NVARCHAR(MAX) NULL,
        NguoiPhanHoi NVARCHAR(MAX) NULL,
        NgayPhanHoi DATETIME2 NULL,
        CONSTRAINT PK_DonDatCocChiTiet PRIMARY KEY (MaChiTiet),
        CONSTRAINT FK_DonDatCocChiTiet_ChiNhanhShowroom_MaChiNhanh FOREIGN KEY (MaChiNhanh) REFERENCES ChiNhanhShowroom(MaChiNhanh) ON DELETE NO ACTION,
        CONSTRAINT FK_DonDatCocChiTiet_DonDatCoc_MaDonCoc FOREIGN KEY (MaDonCoc) REFERENCES DonDatCoc(MaDonCoc) ON DELETE CASCADE,
        CONSTRAINT FK_DonDatCocChiTiet_PhienBanXe_SanPham_MaPhienBan FOREIGN KEY (MaPhienBan) REFERENCES PhienBanXe_SanPham(MaPhienBan) ON DELETE NO ACTION
    );
    CREATE INDEX IX_DonDatCocChiTiet_MaChiNhanh ON DonDatCocChiTiet(MaChiNhanh);
    CREATE INDEX IX_DonDatCocChiTiet_MaDonCoc ON DonDatCocChiTiet(MaDonCoc);
    CREATE INDEX IX_DonDatCocChiTiet_MaPhienBan ON DonDatCocChiTiet(MaPhienBan);
END
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TonKhoTheoChiNhanh')
BEGIN
    CREATE TABLE TonKhoTheoChiNhanh (
        MaTonKho INT IDENTITY(1,1) NOT NULL,
        MaPhienBan INT NOT NULL,
        MaChiNhanh NVARCHAR(450) NOT NULL,
        SoLuong INT NOT NULL,
        NgayCapNhat DATETIME2 NOT NULL,
        CONSTRAINT PK_TonKhoTheoChiNhanh PRIMARY KEY (MaTonKho),
        CONSTRAINT FK_TonKhoTheoChiNhanh_ChiNhanhShowroom_MaChiNhanh FOREIGN KEY (MaChiNhanh) REFERENCES ChiNhanhShowroom(MaChiNhanh) ON DELETE NO ACTION,
        CONSTRAINT FK_TonKhoTheoChiNhanh_PhienBanXe_SanPham_MaPhienBan FOREIGN KEY (MaPhienBan) REFERENCES PhienBanXe_SanPham(MaPhienBan) ON DELETE NO ACTION
    );
    CREATE INDEX IX_TonKhoTheoChiNhanh_MaChiNhanh ON TonKhoTheoChiNhanh(MaChiNhanh);
    CREATE INDEX IX_TonKhoTheoChiNhanh_MaPhienBan ON TonKhoTheoChiNhanh(MaPhienBan);
END
GO

-- 0b. CÁC CỘT TÙY CHỌN CHUYỂN THÀNH NULL (đồng bộ với model: form admin để trống được)
--     Phải chạy trước khối UPSERT để INSERT thiếu cột không bị lỗi NOT NULL.
ALTER TABLE HangXe ALTER COLUMN DuongDanLogo NVARCHAR(MAX) NULL;
ALTER TABLE DongXe ALTER COLUMN KieuDang NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN TenPhienBan NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN MauSac NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN DongCo NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN HopSo NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN LoaiNhietLieu NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN DuongDanAnh NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN MaKhuyenMai NVARCHAR(MAX) NULL;
ALTER TABLE PhienBanXe_SanPham ALTER COLUMN TrangThai NVARCHAR(MAX) NULL;
GO

-- ==================== 1. TÀI KHOẢN (UPSERT - giữ account đã có) ====================
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'fntzzs682@gmail.com')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'fntzzs682@gmail.com',N'123456',N'Admin',N'Active',N'Admin',N'fntzzs682@gmail.com');
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'Ngttu2006@gmail.com')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'Ngttu2006@gmail.com',N'Iumaioanhh@2024',N'Admin',N'Active',N'Ngtnua',N'Ngttu2006@gmail.com');
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'thanhdac223@gmail.com')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'thanhdac223@gmail.com',N'@Trinhvu1',N'Admin',N'Active',N'',N'thanhdac223@gmail.com');
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'Vanh280306@gmail.com')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'Vanh280306@gmail.com',N'Vanh2803',N'Admin',N'Active',N'',N'Vanh280306@gmail.com');
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'minhquanmkp123@gmail.com')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'minhquanmkp123@gmail.com',N'hahahihi123',N'Admin',N'Active',N'',N'minhquanmkp123@gmail.com');
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'user1')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'user1',N'user123',N'User',N'Active',N'Nguyễn Văn User',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'user2')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'user2',N'user123',N'User',N'Active',N'Trần Thị Khách',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'user3')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'user3',N'user123',N'User',N'Active',N'Lê Hoàng Nam',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'user4')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'user4',N'user123',N'User',N'Active',N'Phạm Minh Tâm',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'user5')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'user5',N'user123',N'User',N'Active',N'Đỗ Thúy Hằng',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS1')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS1',N'quanly123',N'Quản Lý',N'Hoạt động',N'Nguyễn Văn A',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS2')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS2',N'quanly123',N'Quản Lý',N'Hoạt động',N'Nguyễn Văn B',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS3')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS3',N'quanly123',N'Quản Lý',N'Hoạt động',N'Trần Thị C',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS4')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS4',N'quanly123',N'Quản Lý',N'Hoạt động',N'Lê Văn D',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS5')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS5',N'quanly123',N'Quản Lý',N'Hoạt động',N'Phạm Thị E',NULL);
IF NOT EXISTS (SELECT 1 FROM TaiKhoan WHERE TenDangNhap = N'QuanlyCS6')
    INSERT INTO TaiKhoan (TenDangNhap,MatKhau,VaiTro,TrangThai,TenHienThi,Email)
    VALUES (N'QuanlyCS6',N'quanly123',N'Quản Lý',N'Hoạt động',N'Hoàng Văn F',NULL);
GO

-- ==================== 2. HÃNG XE (UPSERT - GIỮ NGUYÊN DuongDanLogo trên server) ====================
IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 1)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (1,N'Toyota',N'Nhật Bản',N'https://yt3.googleusercontent.com/Ze3-8domW2lUcA5x0bTN0TNbGGoxLKa_t4l5P-j37BCzuWHf3YlGJwmpZkDJ1M2egKUI4fb5wQ=s900-c-k-c0x00ffffff-no-rj');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Toyota', QuocGia=N'Nhật Bản' WHERE MaHang = 1;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 2)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (2,N'Honda',N'Nhật Bản',N'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRjNN9PyTICXYqFfZwgJSTd_ftng4BTSqxJBFPlBwq19A&s=10');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Honda', QuocGia=N'Nhật Bản' WHERE MaHang = 2;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 3)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (3,N'Ford',N'Mỹ',N'https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRRz3Qk1cyXFVUJ-Ma0HzyswY2YpEecbMonaP7TJ_C9EQ&s=10');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Ford', QuocGia=N'Mỹ' WHERE MaHang = 3;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 4)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (4,N'BMW',N'Đức',N'/uploads/admin/67619976-eaa2-43ce-bfed-be9c65189f45.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'BMW', QuocGia=N'Đức' WHERE MaHang = 4;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 5)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (5,N'VinFast',N'Việt Nam',N'/uploads/admin/a803917d-5cc1-49f4-9a73-930cf7cffd7f.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'VinFast', QuocGia=N'Việt Nam' WHERE MaHang = 5;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 6)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (6,N'Mercedes-Benz',N'Đức',N'/images/brands/mercedes.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Mercedes-Benz', QuocGia=N'Đức' WHERE MaHang = 6;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 7)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (7,N'Audi',N'Đức',N'/images/brands/audi.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Audi', QuocGia=N'Đức' WHERE MaHang = 7;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 8)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (8,N'Lexus',N'Nhật Bản',N'/images/brands/lexus.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Lexus', QuocGia=N'Nhật Bản' WHERE MaHang = 8;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 9)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (9,N'Hyundai',N'Hàn Quốc',N'/images/brands/hyundai.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Hyundai', QuocGia=N'Hàn Quốc' WHERE MaHang = 9;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 10)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (10,N'Kia',N'Hàn Quốc',N'/images/brands/kia.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Kia', QuocGia=N'Hàn Quốc' WHERE MaHang = 10;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 11)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (11,N'Mazda',N'Nhật Bản',N'/images/brands/mazda.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Mazda', QuocGia=N'Nhật Bản' WHERE MaHang = 11;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 12)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (12,N'Suzuki',N'Nhật Bản',N'/images/brands/suzuki.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Suzuki', QuocGia=N'Nhật Bản' WHERE MaHang = 12;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 13)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (13,N'Mitsubishi',N'Nhật Bản',N'/images/brands/mitsubishi.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Mitsubishi', QuocGia=N'Nhật Bản' WHERE MaHang = 13;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 14)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (14,N'Nissan',N'Nhật Bản',N'/images/brands/nissan.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Nissan', QuocGia=N'Nhật Bản' WHERE MaHang = 14;

IF NOT EXISTS (SELECT 1 FROM HangXe WHERE MaHang = 15)
BEGIN
    SET IDENTITY_INSERT HangXe ON;
    INSERT INTO HangXe (MaHang,TenHang,QuocGia,DuongDanLogo) VALUES (15,N'Subaru',N'Nhật Bản',N'/images/brands/subaru.png');
    SET IDENTITY_INSERT HangXe OFF;
END
ELSE
    UPDATE HangXe SET TenHang=N'Subaru', QuocGia=N'Nhật Bản' WHERE MaHang = 15;


GO

-- ==================== 3. DÒNG XE (UPSERT - GIỮ NGUYÊN DuongDanAnh trên server) ====================
IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 1)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (1,1,N'Toyota Camry',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=1, TenDong=N'Toyota Camry', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 1;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 2)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (2,1,N'Toyota Hilux',N'Bán tải',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=1, TenDong=N'Toyota Hilux', KieuDang=N'Bán tải', NoiBat=0 WHERE MaDong = 2;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 3)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (3,1,N'Toyota Corolla Altis',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=1, TenDong=N'Toyota Corolla Altis', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 3;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 4)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (4,1,N'Toyota Fortuner',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=1, TenDong=N'Toyota Fortuner', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 4;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 5)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (5,1,N'Toyota Vios',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=1, TenDong=N'Toyota Vios', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 5;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 6)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (6,2,N'Honda Civic',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=2, TenDong=N'Honda Civic', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 6;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 7)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (7,2,N'Honda CR-V',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=2, TenDong=N'Honda CR-V', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 7;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 8)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (8,2,N'Honda HR-V',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=2, TenDong=N'Honda HR-V', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 8;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 9)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (9,2,N'Honda Accord',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=2, TenDong=N'Honda Accord', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 9;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 10)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (10,3,N'Ford Explorer',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=3, TenDong=N'Ford Explorer', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 10;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 11)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (11,3,N'Ford Everest',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=3, TenDong=N'Ford Everest', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 11;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 12)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (12,3,N'Ford Ranger',N'',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=3, TenDong=N'Ford Ranger', KieuDang=N'', NoiBat=1 WHERE MaDong = 12;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 13)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (13,3,N'Ford Territory',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=3, TenDong=N'Ford Territory', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 13;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 14)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (14,4,N'BMW 3 Series',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=4, TenDong=N'BMW 3 Series', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 14;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 15)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (15,4,N'BMW 5 Series',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=4, TenDong=N'BMW 5 Series', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 15;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 16)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (16,4,N'BMW X3',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=4, TenDong=N'BMW X3', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 16;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 17)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (17,4,N'BMW X5',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=4, TenDong=N'BMW X5', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 17;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 18)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (18,5,N'VinFast Lux SA',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=5, TenDong=N'VinFast Lux SA', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 18;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 19)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (19,5,N'VinFast Fadil',N'Hatchback',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=5, TenDong=N'VinFast Fadil', KieuDang=N'Hatchback', NoiBat=0 WHERE MaDong = 19;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 20)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (20,5,N'VinFast VF e34',N'SUV (Điện)',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=5, TenDong=N'VinFast VF e34', KieuDang=N'SUV (Điện)', NoiBat=0 WHERE MaDong = 20;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 21)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (21,5,N'VinFast VF 8',N'SUV (Điện)',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=5, TenDong=N'VinFast VF 8', KieuDang=N'SUV (Điện)', NoiBat=1 WHERE MaDong = 21;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 22)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (22,5,N'VinFast VF 9',N'SUV (Điện)',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=5, TenDong=N'VinFast VF 9', KieuDang=N'SUV (Điện)', NoiBat=1 WHERE MaDong = 22;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 23)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (23,6,N'Mercedes C-Class',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=6, TenDong=N'Mercedes C-Class', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 23;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 24)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (24,6,N'Mercedes E-Class',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=6, TenDong=N'Mercedes E-Class', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 24;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 25)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (25,6,N'Mercedes S-Class',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=6, TenDong=N'Mercedes S-Class', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 25;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 26)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (26,6,N'Mercedes GLC',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=6, TenDong=N'Mercedes GLC', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 26;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 27)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (27,6,N'Mercedes GLE',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=6, TenDong=N'Mercedes GLE', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 27;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 28)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (28,7,N'Audi A3',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=7, TenDong=N'Audi A3', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 28;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 29)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (29,7,N'Audi A4',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=7, TenDong=N'Audi A4', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 29;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 30)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (30,7,N'Audi Q5',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=7, TenDong=N'Audi Q5', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 30;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 31)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (31,7,N'Audi Q7',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=7, TenDong=N'Audi Q7', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 31;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 32)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (32,8,N'Lexus ES',N'Sedan',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=8, TenDong=N'Lexus ES', KieuDang=N'Sedan', NoiBat=1 WHERE MaDong = 32;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 33)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (33,8,N'Lexus RX',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=8, TenDong=N'Lexus RX', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 33;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 34)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (34,8,N'Lexus NX',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=8, TenDong=N'Lexus NX', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 34;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 35)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (35,9,N'Hyundai Santa Fe',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=9, TenDong=N'Hyundai Santa Fe', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 35;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 36)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (36,9,N'Hyundai Tucson',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=9, TenDong=N'Hyundai Tucson', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 36;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 37)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (37,9,N'Hyundai Accent',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=9, TenDong=N'Hyundai Accent', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 37;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 38)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (38,9,N'Hyundai Creta',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=9, TenDong=N'Hyundai Creta', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 38;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 39)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (39,10,N'Kia Sorento',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=10, TenDong=N'Kia Sorento', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 39;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 40)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (40,10,N'Kia Sportage',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=10, TenDong=N'Kia Sportage', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 40;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 41)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (41,10,N'Kia Cerato',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=10, TenDong=N'Kia Cerato', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 41;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 42)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (42,10,N'Kia Morning',N'Hatchback',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=10, TenDong=N'Kia Morning', KieuDang=N'Hatchback', NoiBat=0 WHERE MaDong = 42;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 43)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (43,11,N'Mazda CX-5',N'SUV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=11, TenDong=N'Mazda CX-5', KieuDang=N'SUV', NoiBat=1 WHERE MaDong = 43;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 44)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (44,11,N'Mazda CX-8',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=11, TenDong=N'Mazda CX-8', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 44;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 45)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (45,11,N'Mazda3',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=11, TenDong=N'Mazda3', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 45;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 46)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (46,11,N'Mazda6',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=11, TenDong=N'Mazda6', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 46;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 47)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (47,12,N'Suzuki Swift',N'Hatchback',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=12, TenDong=N'Suzuki Swift', KieuDang=N'Hatchback', NoiBat=0 WHERE MaDong = 47;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 48)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (48,12,N'Suzuki Vitara',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=12, TenDong=N'Suzuki Vitara', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 48;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 49)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (49,12,N'Suzuki Ertiga',N'MPV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=12, TenDong=N'Suzuki Ertiga', KieuDang=N'MPV', NoiBat=0 WHERE MaDong = 49;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 50)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (50,13,N'Mitsubishi Xpander',N'MPV',1);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=13, TenDong=N'Mitsubishi Xpander', KieuDang=N'MPV', NoiBat=1 WHERE MaDong = 50;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 51)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (51,13,N'Mitsubishi Outlander',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=13, TenDong=N'Mitsubishi Outlander', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 51;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 52)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (52,13,N'Mitsubishi Triton',N'Bán tải',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=13, TenDong=N'Mitsubishi Triton', KieuDang=N'Bán tải', NoiBat=0 WHERE MaDong = 52;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 53)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (53,14,N'Nissan Navara',N'Bán tải',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=14, TenDong=N'Nissan Navara', KieuDang=N'Bán tải', NoiBat=0 WHERE MaDong = 53;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 54)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (54,14,N'Nissan Kicks',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=14, TenDong=N'Nissan Kicks', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 54;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 55)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (55,14,N'Nissan Almera',N'Sedan',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=14, TenDong=N'Nissan Almera', KieuDang=N'Sedan', NoiBat=0 WHERE MaDong = 55;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 56)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (56,15,N'Subaru Forester',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=15, TenDong=N'Subaru Forester', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 56;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 57)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (57,15,N'Subaru Outback',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=15, TenDong=N'Subaru Outback', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 57;

IF NOT EXISTS (SELECT 1 FROM DongXe WHERE MaDong = 58)
BEGIN
    SET IDENTITY_INSERT DongXe ON;
    INSERT INTO DongXe (MaDong,MaHang,TenDong,KieuDang,NoiBat) VALUES (58,15,N'Subaru XV',N'SUV',0);
    SET IDENTITY_INSERT DongXe OFF;
END
ELSE
    UPDATE DongXe SET MaHang=15, TenDong=N'Subaru XV', KieuDang=N'SUV', NoiBat=0 WHERE MaDong = 58;


GO

-- ==================== 4. PHIÊN BẢN XE (UPSERT - GIỮ NGUYÊN DuongDanAnh trên server) ====================
IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 1)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (1,1,N'Camry 2.0 CVT',1050000000,N'Bạc',N'2.0L 4 xi-lanh',N'CVT',N'Xăng',N'/images/cars/camry.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=1, TenPhienBan=N'Camry 2.0 CVT', GiaNiemYet=1050000000, MauSac=N'Bạc', DongCo=N'2.0L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 1;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 2)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (2,1,N'Camry 2.5 HEV',1250000000,N'Đen',N'2.5L Hybrid',N'e-CVT',N'Xăng + Điện',N'/images/cars/camry-hybrid.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=1, TenPhienBan=N'Camry 2.5 HEV', GiaNiemYet=1250000000, MauSac=N'Đen', DongCo=N'2.5L Hybrid', HopSo=N'e-CVT', LoaiNhietLieu=N'Xăng + Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 2;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 3)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (3,2,N'Hilux 2.4G 4x2 AT',950000000,N'Trắng',N'2.4L Turbo Diesel',N'AT 6 cấp',N'Dầu',N'/images/cars/hilux.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=2, TenPhienBan=N'Hilux 2.4G 4x2 AT', GiaNiemYet=950000000, MauSac=N'Trắng', DongCo=N'2.4L Turbo Diesel', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 3;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 4)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (4,2,N'Hilux 2.8G 4x4 AT',1150000000,N'Xám',N'2.8L Turbo Diesel',N'AT 6 cấp',N'Dầu',N'/images/cars/hilux.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=2, TenPhienBan=N'Hilux 2.8G 4x4 AT', GiaNiemYet=1150000000, MauSac=N'Xám', DongCo=N'2.8L Turbo Diesel', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 4;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 5)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (5,3,N'Corolla Altis 1.8G CVT',750000000,N'Đen',N'1.8L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=3, TenPhienBan=N'Corolla Altis 1.8G CVT', GiaNiemYet=750000000, MauSac=N'Đen', DongCo=N'1.8L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 5;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 6)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (6,3,N'Corolla Altis 1.8HEV',850000000,N'Xanh',N'1.8L Hybrid',N'e-CVT',N'Xăng + Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=3, TenPhienBan=N'Corolla Altis 1.8HEV', GiaNiemYet=850000000, MauSac=N'Xanh', DongCo=N'1.8L Hybrid', HopSo=N'e-CVT', LoaiNhietLieu=N'Xăng + Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 6;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 7)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (7,4,N'Fortuner 2.4G 4x2 AT',1100000000,N'Trắng',N'2.4L Turbo Diesel',N'AT 6 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=4, TenPhienBan=N'Fortuner 2.4G 4x2 AT', GiaNiemYet=1100000000, MauSac=N'Trắng', DongCo=N'2.4L Turbo Diesel', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 7;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 8)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (8,4,N'Fortuner 2.8V 4x4 AT',1350000000,N'Đen',N'2.8L Turbo Diesel',N'AT 6 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=4, TenPhienBan=N'Fortuner 2.8V 4x4 AT', GiaNiemYet=1350000000, MauSac=N'Đen', DongCo=N'2.8L Turbo Diesel', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 8;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 9)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (9,5,N'Vios 1.5G CVT',530000000,N'Bạc',N'1.5L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=5, TenPhienBan=N'Vios 1.5G CVT', GiaNiemYet=530000000, MauSac=N'Bạc', DongCo=N'1.5L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 9;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 10)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (10,5,N'Vios 1.5E MT',470000000,N'Đỏ',N'1.5L 4 xi-lanh',N'MT 5 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=5, TenPhienBan=N'Vios 1.5E MT', GiaNiemYet=470000000, MauSac=N'Đỏ', DongCo=N'1.5L 4 xi-lanh', HopSo=N'MT 5 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 10;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 11)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (11,6,N'Civic 1.5 Turbo CVT',850000000,N'Đỏ',N'1.5L Turbo',N'CVT',N'Xăng',N'/images/cars/civic.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=6, TenPhienBan=N'Civic 1.5 Turbo CVT', GiaNiemYet=850000000, MauSac=N'Đỏ', DongCo=N'1.5L Turbo', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 11;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 12)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (12,6,N'Civic RS 1.5 Turbo',920000000,N'Đen',N'1.5L Turbo',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=6, TenPhienBan=N'Civic RS 1.5 Turbo', GiaNiemYet=920000000, MauSac=N'Đen', DongCo=N'1.5L Turbo', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 12;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 13)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (13,7,N'CR-V 1.5 Turbo G',1050000000,N'Xanh',N'1.5L Turbo',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=7, TenPhienBan=N'CR-V 1.5 Turbo G', GiaNiemYet=1050000000, MauSac=N'Xanh', DongCo=N'1.5L Turbo', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 13;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 14)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (14,7,N'CR-V 1.5 Turbo L',1200000000,N'Xám',N'1.5L Turbo',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=7, TenPhienBan=N'CR-V 1.5 Turbo L', GiaNiemYet=1200000000, MauSac=N'Xám', DongCo=N'1.5L Turbo', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 14;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 15)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (15,8,N'HR-V 1.8L',750000000,N'Trắng',N'1.8L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=8, TenPhienBan=N'HR-V 1.8L', GiaNiemYet=750000000, MauSac=N'Trắng', DongCo=N'1.8L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 15;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 16)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (16,9,N'Accord 2.0 Turbo',1400000000,N'Đen',N'2.0L Turbo',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=9, TenPhienBan=N'Accord 2.0 Turbo', GiaNiemYet=1400000000, MauSac=N'Đen', DongCo=N'2.0L Turbo', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 16;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 17)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (17,10,N'Explorer 2.3L EcoBoost',1200000000,N'Xanh',N'2.3L EcoBoost',N'AT 10 cấp',N'Xăng',N'/images/cars/explorer.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=10, TenPhienBan=N'Explorer 2.3L EcoBoost', GiaNiemYet=1200000000, MauSac=N'Xanh', DongCo=N'2.3L EcoBoost', HopSo=N'AT 10 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 17;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 18)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (18,10,N'Explorer 3.0L V6',1500000000,N'Đen',N'3.0L V6 EcoBoost',N'AT 10 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=10, TenPhienBan=N'Explorer 3.0L V6', GiaNiemYet=1500000000, MauSac=N'Đen', DongCo=N'3.0L V6 EcoBoost', HopSo=N'AT 10 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 18;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 19)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (19,11,N'Everest 2.0L Turbo 4x2',1100000000,N'Trắng',N'2.0L Turbo Diesel',N'AT 10 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=11, TenPhienBan=N'Everest 2.0L Turbo 4x2', GiaNiemYet=1100000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo Diesel', HopSo=N'AT 10 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 19;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 20)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (20,11,N'Everest 2.0L Turbo 4x4',1300000000,N'Xám',N'2.0L Turbo Diesel',N'AT 10 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=11, TenPhienBan=N'Everest 2.0L Turbo 4x4', GiaNiemYet=1300000000, MauSac=N'Xám', DongCo=N'2.0L Turbo Diesel', HopSo=N'AT 10 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 20;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 21)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (21,12,N'Ranger 2.0L XL 4x2',700000000,N'Trắng',N'2.0L Turbo Diesel',N'AT 6 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=12, TenPhienBan=N'Ranger 2.0L XL 4x2', GiaNiemYet=700000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo Diesel', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 21;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 22)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (22,12,N'Ranger 2.0L Wildtrak 4x4',950000000,N'Đỏ',N'2.0L Turbo Diesel',N'AT 10 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=12, TenPhienBan=N'Ranger 2.0L Wildtrak 4x4', GiaNiemYet=950000000, MauSac=N'Đỏ', DongCo=N'2.0L Turbo Diesel', HopSo=N'AT 10 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 22;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 23)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (23,13,N'Territory 1.8L Titanium',850000000,N'Xanh',N'1.8L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=13, TenPhienBan=N'Territory 1.8L Titanium', GiaNiemYet=850000000, MauSac=N'Xanh', DongCo=N'1.8L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 23;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 24)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (24,14,N'320i Sport Line',1600000000,N'Đen',N'2.0L Turbo 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'/images/cars/bmw320.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=14, TenPhienBan=N'320i Sport Line', GiaNiemYet=1600000000, MauSac=N'Đen', DongCo=N'2.0L Turbo 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 24;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 25)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (25,14,N'330i M Sport',1900000000,N'Xanh',N'2.0L Turbo 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=14, TenPhienBan=N'330i M Sport', GiaNiemYet=1900000000, MauSac=N'Xanh', DongCo=N'2.0L Turbo 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 25;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 26)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (26,15,N'520i Luxury',2300000000,N'Bạc',N'2.0L Turbo 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=15, TenPhienBan=N'520i Luxury', GiaNiemYet=2300000000, MauSac=N'Bạc', DongCo=N'2.0L Turbo 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 26;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 27)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (27,15,N'530i M Sport',2700000000,N'Đen',N'3.0L Turbo 6 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=15, TenPhienBan=N'530i M Sport', GiaNiemYet=2700000000, MauSac=N'Đen', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 27;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 28)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (28,16,N'X3 xDrive20i',2000000000,N'Trắng',N'2.0L Turbo 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=16, TenPhienBan=N'X3 xDrive20i', GiaNiemYet=2000000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 28;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 29)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (29,16,N'X3 M40i',2800000000,N'Xanh',N'3.0L Turbo 6 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=16, TenPhienBan=N'X3 M40i', GiaNiemYet=2800000000, MauSac=N'Xanh', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 29;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 30)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (30,17,N'X5 xDrive40i',3500000000,N'Đen',N'3.0L Turbo 6 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=17, TenPhienBan=N'X5 xDrive40i', GiaNiemYet=3500000000, MauSac=N'Đen', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 30;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 31)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (31,18,N'Lux SA 2.0T Base',1100000000,N'Trắng',N'2.0L Turbo',N'AT 8 cấp',N'Xăng',N'/images/cars/luxsa.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=18, TenPhienBan=N'Lux SA 2.0T Base', GiaNiemYet=1100000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 31;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 32)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (32,18,N'Lux SA 2.0T Premium',1300000000,N'Đen',N'2.0L Turbo',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=18, TenPhienBan=N'Lux SA 2.0T Premium', GiaNiemYet=1300000000, MauSac=N'Đen', DongCo=N'2.0L Turbo', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 32;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 33)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (33,19,N'Fadil 1.2 Base',360000000,N'Đỏ',N'1.2L 3 xi-lanh',N'MT 5 cấp',N'Xăng',N'/images/cars/fadil.jpg',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=19, TenPhienBan=N'Fadil 1.2 Base', GiaNiemYet=360000000, MauSac=N'Đỏ', DongCo=N'1.2L 3 xi-lanh', HopSo=N'MT 5 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 33;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 34)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (34,19,N'Fadil 1.2 AT',395000000,N'Bạc',N'1.2L 3 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=19, TenPhienBan=N'Fadil 1.2 AT', GiaNiemYet=395000000, MauSac=N'Bạc', DongCo=N'1.2L 3 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 34;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 35)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (35,20,N'VF e34 Eco',710000000,N'Xanh',N'Điện động cơ 110kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=20, TenPhienBan=N'VF e34 Eco', GiaNiemYet=710000000, MauSac=N'Xanh', DongCo=N'Điện động cơ 110kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 35;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 36)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (36,20,N'VF e34 Plus',770000000,N'Trắng',N'Điện động cơ 110kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=20, TenPhienBan=N'VF e34 Plus', GiaNiemYet=770000000, MauSac=N'Trắng', DongCo=N'Điện động cơ 110kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 36;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 37)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (37,21,N'VF 8 Eco',1050000000,N'Xanh',N'Điện động cơ 260kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=21, TenPhienBan=N'VF 8 Eco', GiaNiemYet=1050000000, MauSac=N'Xanh', DongCo=N'Điện động cơ 260kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 37;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 38)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (38,21,N'VF 8 Plus',1150000000,N'Đỏ',N'Điện động cơ 300kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=21, TenPhienBan=N'VF 8 Plus', GiaNiemYet=1150000000, MauSac=N'Đỏ', DongCo=N'Điện động cơ 300kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 38;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 39)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (39,22,N'VF 9 Eco',1600000000,N'Bạc',N'Điện động cơ 300kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=22, TenPhienBan=N'VF 9 Eco', GiaNiemYet=1600000000, MauSac=N'Bạc', DongCo=N'Điện động cơ 300kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 39;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 40)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (40,22,N'VF 9 Plus',1800000000,N'Đen',N'Điện động cơ 300kW',N'1 cấp',N'Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=22, TenPhienBan=N'VF 9 Plus', GiaNiemYet=1800000000, MauSac=N'Đen', DongCo=N'Điện động cơ 300kW', HopSo=N'1 cấp', LoaiNhietLieu=N'Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 40;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 41)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (41,23,N'C200 Avantgarde',1500000000,N'Bạc',N'1.5L Turbo + 48V',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=23, TenPhienBan=N'C200 Avantgarde', GiaNiemYet=1500000000, MauSac=N'Bạc', DongCo=N'1.5L Turbo + 48V', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 41;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 42)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (42,23,N'C300 AMG Line',1850000000,N'Đen',N'2.0L Turbo',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=23, TenPhienBan=N'C300 AMG Line', GiaNiemYet=1850000000, MauSac=N'Đen', DongCo=N'2.0L Turbo', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 42;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 43)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (43,24,N'E200 Exclusive',2100000000,N'Trắng',N'2.0L Turbo',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=24, TenPhienBan=N'E200 Exclusive', GiaNiemYet=2100000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 43;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 44)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (44,24,N'E300 AMG Line',2500000000,N'Xanh',N'2.0L Turbo',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=24, TenPhienBan=N'E300 AMG Line', GiaNiemYet=2500000000, MauSac=N'Xanh', DongCo=N'2.0L Turbo', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 44;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 45)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (45,25,N'S450L 4MATIC',5500000000,N'Đen',N'3.0L Turbo 6 xi-lanh',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=25, TenPhienBan=N'S450L 4MATIC', GiaNiemYet=5500000000, MauSac=N'Đen', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 45;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 46)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (46,26,N'GLC 200 4MATIC',1900000000,N'Xám',N'2.0L Turbo',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=26, TenPhienBan=N'GLC 200 4MATIC', GiaNiemYet=1900000000, MauSac=N'Xám', DongCo=N'2.0L Turbo', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 46;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 47)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (47,26,N'GLC 300 4MATIC',2300000000,N'Trắng',N'2.0L Turbo',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=26, TenPhienBan=N'GLC 300 4MATIC', GiaNiemYet=2300000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 47;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 48)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (48,27,N'GLE 300d 4MATIC',3000000000,N'Đen',N'3.0L Turbo Diesel',N'AT 9 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=27, TenPhienBan=N'GLE 300d 4MATIC', GiaNiemYet=3000000000, MauSac=N'Đen', DongCo=N'3.0L Turbo Diesel', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 48;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 49)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (49,27,N'GLE 450 4MATIC',3700000000,N'Xanh',N'3.0L Turbo 6 xi-lanh',N'AT 9 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=27, TenPhienBan=N'GLE 450 4MATIC', GiaNiemYet=3700000000, MauSac=N'Xanh', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 9 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 49;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 50)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (50,28,N'A3 35 TFSI',1250000000,N'Bạc',N'1.4L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=28, TenPhienBan=N'A3 35 TFSI', GiaNiemYet=1250000000, MauSac=N'Bạc', DongCo=N'1.4L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 50;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 51)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (51,29,N'A4 40 TFSI',1600000000,N'Đen',N'2.0L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=29, TenPhienBan=N'A4 40 TFSI', GiaNiemYet=1600000000, MauSac=N'Đen', DongCo=N'2.0L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 51;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 52)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (52,29,N'A4 45 TFSI Quattro',1850000000,N'Trắng',N'2.0L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=29, TenPhienBan=N'A4 45 TFSI Quattro', GiaNiemYet=1850000000, MauSac=N'Trắng', DongCo=N'2.0L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 52;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 53)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (53,30,N'Q5 40 TFSI',2200000000,N'Xanh',N'2.0L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=30, TenPhienBan=N'Q5 40 TFSI', GiaNiemYet=2200000000, MauSac=N'Xanh', DongCo=N'2.0L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 53;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 54)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (54,31,N'Q7 45 TFSI Quattro',3200000000,N'Đen',N'3.0L Turbo 6 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=31, TenPhienBan=N'Q7 45 TFSI Quattro', GiaNiemYet=3200000000, MauSac=N'Đen', DongCo=N'3.0L Turbo 6 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 54;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 55)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (55,32,N'ES 250',2300000000,N'Bạc',N'2.5L 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=32, TenPhienBan=N'ES 250', GiaNiemYet=2300000000, MauSac=N'Bạc', DongCo=N'2.5L 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 55;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 56)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (56,33,N'RX 350 F Sport',3300000000,N'Đen',N'3.5L V6',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=33, TenPhienBan=N'RX 350 F Sport', GiaNiemYet=3300000000, MauSac=N'Đen', DongCo=N'3.5L V6', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 56;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 57)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (57,34,N'NX 250',2100000000,N'Xám',N'2.5L 4 xi-lanh',N'AT 8 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=34, TenPhienBan=N'NX 250', GiaNiemYet=2100000000, MauSac=N'Xám', DongCo=N'2.5L 4 xi-lanh', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 57;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 58)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (58,34,N'NX 350h',2400000000,N'Xanh',N'2.5L Hybrid',N'e-CVT',N'Xăng + Điện',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=34, TenPhienBan=N'NX 350h', GiaNiemYet=2400000000, MauSac=N'Xanh', DongCo=N'2.5L Hybrid', HopSo=N'e-CVT', LoaiNhietLieu=N'Xăng + Điện', MaKhuyenMai=N'' WHERE MaPhienBan = 58;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 59)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (59,35,N'Santa Fe 2.5 Premium',1150000000,N'Xanh',N'2.5L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=35, TenPhienBan=N'Santa Fe 2.5 Premium', GiaNiemYet=1150000000, MauSac=N'Xanh', DongCo=N'2.5L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 59;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 60)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (60,35,N'Santa Fe 2.2D Calligraphy',1350000000,N'Đen',N'2.2L Turbo Diesel',N'AT 8 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=35, TenPhienBan=N'Santa Fe 2.2D Calligraphy', GiaNiemYet=1350000000, MauSac=N'Đen', DongCo=N'2.2L Turbo Diesel', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 60;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 61)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (61,36,N'Tucson 2.0X Tiêu chuẩn',750000000,N'Trắng',N'2.0L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=36, TenPhienBan=N'Tucson 2.0X Tiêu chuẩn', GiaNiemYet=750000000, MauSac=N'Trắng', DongCo=N'2.0L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 61;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 62)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (62,36,N'Tucson 1.6T Đặc biệt',900000000,N'Đỏ',N'1.6L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=36, TenPhienBan=N'Tucson 1.6T Đặc biệt', GiaNiemYet=900000000, MauSac=N'Đỏ', DongCo=N'1.6L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 62;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 63)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (63,37,N'Accent 1.4MT Base',430000000,N'Bạc',N'1.4L 4 xi-lanh',N'MT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=37, TenPhienBan=N'Accent 1.4MT Base', GiaNiemYet=430000000, MauSac=N'Bạc', DongCo=N'1.4L 4 xi-lanh', HopSo=N'MT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 63;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 64)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (64,37,N'Accent 1.4AT Đặc biệt',500000000,N'Đen',N'1.4L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=37, TenPhienBan=N'Accent 1.4AT Đặc biệt', GiaNiemYet=500000000, MauSac=N'Đen', DongCo=N'1.4L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 64;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 65)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (65,38,N'Creta 1.5 Tiêu chuẩn',640000000,N'Xanh',N'1.5L 4 xi-lanh',N'MT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=38, TenPhienBan=N'Creta 1.5 Tiêu chuẩn', GiaNiemYet=640000000, MauSac=N'Xanh', DongCo=N'1.5L 4 xi-lanh', HopSo=N'MT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 65;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 66)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (66,38,N'Creta 1.5 Đặc biệt',720000000,N'Trắng',N'1.5L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=38, TenPhienBan=N'Creta 1.5 Đặc biệt', GiaNiemYet=720000000, MauSac=N'Trắng', DongCo=N'1.5L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 66;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 67)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (67,39,N'Sorento 2.5 X-Line',1250000000,N'Đen',N'2.5L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=39, TenPhienBan=N'Sorento 2.5 X-Line', GiaNiemYet=1250000000, MauSac=N'Đen', DongCo=N'2.5L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 67;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 68)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (68,39,N'Sorento 2.2D Premium',1450000000,N'Xám',N'2.2L Turbo Diesel',N'AT 8 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=39, TenPhienBan=N'Sorento 2.2D Premium', GiaNiemYet=1450000000, MauSac=N'Xám', DongCo=N'2.2L Turbo Diesel', HopSo=N'AT 8 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 68;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 69)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (69,40,N' Sportage 2.0 Tiêu chuẩn',800000000,N'Trắng',N'2.0L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=40, TenPhienBan=N' Sportage 2.0 Tiêu chuẩn', GiaNiemYet=800000000, MauSac=N'Trắng', DongCo=N'2.0L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 69;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 70)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (70,40,N' Sportage 1.6T GT-Line',1000000000,N'Xanh',N'1.6L Turbo',N'AT 7 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=40, TenPhienBan=N' Sportage 1.6T GT-Line', GiaNiemYet=1000000000, MauSac=N'Xanh', DongCo=N'1.6L Turbo', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 70;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 71)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (71,41,N'Cerato 1.6MT',580000000,N'Bạc',N'1.6L 4 xi-lanh',N'MT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=41, TenPhienBan=N'Cerato 1.6MT', GiaNiemYet=580000000, MauSac=N'Bạc', DongCo=N'1.6L 4 xi-lanh', HopSo=N'MT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 71;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 72)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (72,41,N'Cerato 2.0AT',680000000,N'Đỏ',N'2.0L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=41, TenPhienBan=N'Cerato 2.0AT', GiaNiemYet=680000000, MauSac=N'Đỏ', DongCo=N'2.0L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 72;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 73)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (73,42,N'Morning 1.25MT',360000000,N'Đỏ',N'1.25L 4 xi-lanh',N'MT 5 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=42, TenPhienBan=N'Morning 1.25MT', GiaNiemYet=360000000, MauSac=N'Đỏ', DongCo=N'1.25L 4 xi-lanh', HopSo=N'MT 5 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 73;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 74)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (74,42,N'Morning 1.25AT',400000000,N'Trắng',N'1.25L 4 xi-lanh',N'AT 4 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=42, TenPhienBan=N'Morning 1.25AT', GiaNiemYet=400000000, MauSac=N'Trắng', DongCo=N'1.25L 4 xi-lanh', HopSo=N'AT 4 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 74;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 75)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (75,43,N'CX-5 2.0L Deluxe',850000000,N'Đỏ',N'2.0L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=43, TenPhienBan=N'CX-5 2.0L Deluxe', GiaNiemYet=850000000, MauSac=N'Đỏ', DongCo=N'2.0L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 75;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 76)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (76,43,N'CX-5 2.5L Signature',1050000000,N'Xanh',N'2.5L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=43, TenPhienBan=N'CX-5 2.5L Signature', GiaNiemYet=1050000000, MauSac=N'Xanh', DongCo=N'2.5L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 76;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 77)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (77,44,N'CX-8 2.5L Premium',1150000000,N'Đen',N'2.5L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=44, TenPhienBan=N'CX-8 2.5L Premium', GiaNiemYet=1150000000, MauSac=N'Đen', DongCo=N'2.5L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 77;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 78)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (78,45,N'Mazda3 1.5L Deluxe',620000000,N'Bạc',N'1.5L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=45, TenPhienBan=N'Mazda3 1.5L Deluxe', GiaNiemYet=620000000, MauSac=N'Bạc', DongCo=N'1.5L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 78;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 79)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (79,45,N'Mazda3 2.0L Premium',720000000,N'Đỏ',N'2.0L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=45, TenPhienBan=N'Mazda3 2.0L Premium', GiaNiemYet=720000000, MauSac=N'Đỏ', DongCo=N'2.0L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 79;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 80)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (80,46,N'Mazda6 2.0L Deluxe',850000000,N'Xám',N'2.0L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=46, TenPhienBan=N'Mazda6 2.0L Deluxe', GiaNiemYet=850000000, MauSac=N'Xám', DongCo=N'2.0L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 80;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 81)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (81,46,N'Mazda6 2.5L Premium',1000000000,N'Đen',N'2.5L SkyActiv',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=46, TenPhienBan=N'Mazda6 2.5L Premium', GiaNiemYet=1000000000, MauSac=N'Đen', DongCo=N'2.5L SkyActiv', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 81;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 82)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (82,47,N'Swift 1.2L AT',480000000,N'Xanh',N'1.2L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=47, TenPhienBan=N'Swift 1.2L AT', GiaNiemYet=480000000, MauSac=N'Xanh', DongCo=N'1.2L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 82;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 83)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (83,48,N'Vitara 1.6L AT',680000000,N'Xám',N'1.6L 4 xi-lanh',N'AT 6 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=48, TenPhienBan=N'Vitara 1.6L AT', GiaNiemYet=680000000, MauSac=N'Xám', DongCo=N'1.6L 4 xi-lanh', HopSo=N'AT 6 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 83;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 84)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (84,49,N'Ertiga 1.5L AT',580000000,N'Trắng',N'1.5L 4 xi-lanh',N'AT 4 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=49, TenPhienBan=N'Ertiga 1.5L AT', GiaNiemYet=580000000, MauSac=N'Trắng', DongCo=N'1.5L 4 xi-lanh', HopSo=N'AT 4 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 84;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 85)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (85,50,N'Xpander 1.5L AT',660000000,N'Bạc',N'1.5L 4 xi-lanh',N'AT 4 cấp',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=50, TenPhienBan=N'Xpander 1.5L AT', GiaNiemYet=660000000, MauSac=N'Bạc', DongCo=N'1.5L 4 xi-lanh', HopSo=N'AT 4 cấp', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 85;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 86)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (86,51,N'Outlander 2.0L CVT',950000000,N'Xanh',N'2.0L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=51, TenPhienBan=N'Outlander 2.0L CVT', GiaNiemYet=950000000, MauSac=N'Xanh', DongCo=N'2.0L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 86;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 87)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (87,52,N'Triton 2.4L 4x2 AT',720000000,N'Trắng',N'2.4L Turbo Diesel',N'AT 5 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=52, TenPhienBan=N'Triton 2.4L 4x2 AT', GiaNiemYet=720000000, MauSac=N'Trắng', DongCo=N'2.4L Turbo Diesel', HopSo=N'AT 5 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 87;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 88)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (88,52,N'Triton 2.4L 4x4 AT',850000000,N'Đen',N'2.4L Turbo Diesel',N'AT 5 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=52, TenPhienBan=N'Triton 2.4L 4x4 AT', GiaNiemYet=850000000, MauSac=N'Đen', DongCo=N'2.4L Turbo Diesel', HopSo=N'AT 5 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 88;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 89)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (89,53,N'Navara 2.5L 4x2 AT',750000000,N'Trắng',N'2.5L Turbo Diesel',N'AT 7 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=53, TenPhienBan=N'Navara 2.5L 4x2 AT', GiaNiemYet=750000000, MauSac=N'Trắng', DongCo=N'2.5L Turbo Diesel', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 89;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 90)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (90,53,N'Navara 2.5L 4x4 AT',950000000,N'Đỏ',N'2.5L Turbo Diesel',N'AT 7 cấp',N'Dầu',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=53, TenPhienBan=N'Navara 2.5L 4x4 AT', GiaNiemYet=950000000, MauSac=N'Đỏ', DongCo=N'2.5L Turbo Diesel', HopSo=N'AT 7 cấp', LoaiNhietLieu=N'Dầu', MaKhuyenMai=N'' WHERE MaPhienBan = 90;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 91)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (91,54,N'Kicks 1.2L AT',680000000,N'Xanh',N'1.2L 3 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=54, TenPhienBan=N'Kicks 1.2L AT', GiaNiemYet=680000000, MauSac=N'Xanh', DongCo=N'1.2L 3 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 91;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 92)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (92,55,N'Almera 1.5L AT',550000000,N'Bạc',N'1.5L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=55, TenPhienBan=N'Almera 1.5L AT', GiaNiemYet=550000000, MauSac=N'Bạc', DongCo=N'1.5L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 92;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 93)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (93,56,N'Forester 2.0L i-L',1150000000,N'Xanh',N'2.0L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=56, TenPhienBan=N'Forester 2.0L i-L', GiaNiemYet=1150000000, MauSac=N'Xanh', DongCo=N'2.0L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 93;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 94)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (94,56,N'Forester 2.0L i-S',1300000000,N'Xám',N'2.0L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=56, TenPhienBan=N'Forester 2.0L i-S', GiaNiemYet=1300000000, MauSac=N'Xám', DongCo=N'2.0L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 94;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 95)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (95,57,N'Outback 2.5L',1700000000,N'Đen',N'2.5L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=57, TenPhienBan=N'Outback 2.5L', GiaNiemYet=1700000000, MauSac=N'Đen', DongCo=N'2.5L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 95;

IF NOT EXISTS (SELECT 1 FROM PhienBanXe_SanPham WHERE MaPhienBan = 96)
BEGIN
    SET IDENTITY_INSERT PhienBanXe_SanPham ON;
    INSERT INTO PhienBanXe_SanPham (MaPhienBan,MaDong,TenPhienBan,GiaNiemYet,MauSac,DongCo,HopSo,LoaiNhietLieu,DuongDanAnh,MaKhuyenMai) VALUES (96,58,N'XV 2.0L',900000000,N'Xanh',N'2.0L 4 xi-lanh',N'CVT',N'Xăng',N'',N'');
    SET IDENTITY_INSERT PhienBanXe_SanPham OFF;
END
ELSE
    UPDATE PhienBanXe_SanPham SET MaDong=58, TenPhienBan=N'XV 2.0L', GiaNiemYet=900000000, MauSac=N'Xanh', DongCo=N'2.0L 4 xi-lanh', HopSo=N'CVT', LoaiNhietLieu=N'Xăng', MaKhuyenMai=N'' WHERE MaPhienBan = 96;


GO

-- ==================== 5. CHI NHÁNH (MERGE) ====================
MERGE INTO ChiNhanhShowroom AS T
USING (VALUES
 (N'CN01',N'Showroom TP. Hồ Chí Minh (Cơ sở 1)',N'123 Nguyễn Văn Linh, P. Tân Phong, Quận 7',N'TP. Hồ Chí Minh',N'0909123456',N'QuanlyCS1',N'Hoạt động'), (N'CN02',N'Showroom TP. Hồ Chí Minh (Cơ sở 2)',N'456 Xa lộ Hà Nội, P. Bình Thọ, TP. Thủ Đức',N'TP. Hồ Chí Minh',N'0911222333',N'QuanlyCS2',N'Hoạt động'), (N'CN03',N'Showroom Hà Nội (Cơ sở 3)',N'789 Trần Duy Hưng, P. Trung Hòa, Q. Cầu Giấy',N'Hà Nội',N'0922333444',N'QuanlyCS3',N'Hoạt động'), (N'CN04',N'Showroom Hà Nội (Cơ sở 4)',N'321 Giải Phóng, P. Hoàng Văn Thụ, Q. Hoàng Mai',N'Hà Nội',N'0933444555',N'QuanlyCS4',N'Hoạt động'), (N'CN05',N'Showroom Đà Nẵng (Cơ sở 5)',N'654 Nguyễn Văn Linh, P. Khuê Trung, Q. Hải Châu',N'Đà Nẵng',N'0944555666',N'QuanlyCS5',N'Hoạt động'), (N'CN06',N'Showroom Hải Phòng (Cơ sở 6)',N'987 Võ Nguyên Giáp, P. Vĩnh Niệm, Q. Lê Chân',N'Hải Phòng',N'0955666777',N'QuanlyCS6',N'Hoạt động')
) AS S(MaChiNhanh,TenChiNhanh,DiaChi,ThanhPho,DuongDayNong,MaQuanLy,TrangThai)
ON T.MaChiNhanh = S.MaChiNhanh
WHEN MATCHED THEN UPDATE SET TenChiNhanh=S.TenChiNhanh, DiaChi=S.DiaChi, ThanhPho=S.ThanhPho, DuongDayNong=S.DuongDayNong, MaQuanLy=S.MaQuanLy, TrangThai=S.TrangThai
WHEN NOT MATCHED THEN INSERT (MaChiNhanh,TenChiNhanh,DiaChi,ThanhPho,DuongDayNong,MaQuanLy,TrangThai) VALUES (S.MaChiNhanh,S.TenChiNhanh,S.DiaChi,S.ThanhPho,S.DuongDayNong,S.MaQuanLy,S.TrangThai);
GO

-- ==================== 6. KHUYẾN MÃI (MERGE) ====================
MERGE INTO ChuongTrinhKhuyenMai AS T
USING (VALUES
 (N'KM01',N'Giảm 50% lệ phí trước bạ',N'Áp dụng cho tất cả dòng xe',N'Phần trăm',50.00,50000000.00,'2024-01-01','2024-03-31',N'Hoạt động'), (N'KM02',N'Giảm ngay 20 triệu',N'Cho dòng xe Toyota Vios và Hyundai Accent',N'Số tiền',20000000.00,20000000.00,'2026-07-01','2026-09-30',N'Hoạt động'), (N'KM03',N'Tặng gói phụ kiện 15 triệu',N'Cho khách đặt cọc xe Mercedes trước 30/09',N'Số tiền',15000000.00,15000000.00,'2026-07-01','2026-09-30',N'Hoạt động'), (N'KM04',N'Giảm 10% cho xe điện VinFast',N'Áp dụng cho VF e34, VF 8, VF 9',N'Phần trăm',10.00,150000000.00,'2026-08-01','2026-12-31',N'Hoạt động')
) AS S(MaKhuyenMai,TieuDe,MoTa,LoaiGiamGia,GiaTriGiam,MucGiamToiDa,NgayBatDau,NgayKetThuc,TrangThai)
ON T.MaKhuyenMai = S.MaKhuyenMai
WHEN MATCHED THEN UPDATE SET TieuDe=S.TieuDe, MoTa=S.MoTa, LoaiGiamGia=S.LoaiGiamGia, GiaTriGiam=S.GiaTriGiam, MucGiamToiDa=S.MucGiamToiDa, NgayBatDau=S.NgayBatDau, NgayKetThuc=S.NgayKetThuc, TrangThai=S.TrangThai
WHEN NOT MATCHED THEN INSERT (MaKhuyenMai,TieuDe,MoTa,LoaiGiamGia,GiaTriGiam,MucGiamToiDa,NgayBatDau,NgayKetThuc,TrangThai) VALUES (S.MaKhuyenMai,S.TieuDe,S.MoTa,S.LoaiGiamGia,S.GiaTriGiam,S.MucGiamToiDa,S.NgayBatDau,S.NgayKetThuc,S.TrangThai);
GO

-- ==================== 7. BANNER (UPSERT - GIỮ NGUYÊN DuongDanAnh banner trên server) ====================
IF NOT EXISTS (SELECT 1 FROM QuangCaoBanner WHERE MaBanner = 1002)
BEGIN
    SET IDENTITY_INSERT QuangCaoBanner ON;
    INSERT INTO QuangCaoBanner (MaBanner,DuongDanAnh,DuongDanLienKet,ThuTuHienThi,MaQuanLyCapNhat,TrangThaiKichHoat) VALUES (1002,N'/uploads/admin/91e457aa-b6db-46b6-aae7-776490397f8a.jpg',N'',1,N'fntzzs682@gmail.com',1);
    SET IDENTITY_INSERT QuangCaoBanner OFF;
END
ELSE
    UPDATE QuangCaoBanner SET DuongDanLienKet=N'', ThuTuHienThi=1, TrangThaiKichHoat=1 WHERE MaBanner = 1002;

IF NOT EXISTS (SELECT 1 FROM QuangCaoBanner WHERE MaBanner = 1006)
BEGIN
    SET IDENTITY_INSERT QuangCaoBanner ON;
    INSERT INTO QuangCaoBanner (MaBanner,DuongDanAnh,DuongDanLienKet,ThuTuHienThi,MaQuanLyCapNhat,TrangThaiKichHoat) VALUES (1006,N'/images/banners/banner5.jpg',N'',5,N'fntzzs682@gmail.com',1);
    SET IDENTITY_INSERT QuangCaoBanner OFF;
END
ELSE
    UPDATE QuangCaoBanner SET DuongDanLienKet=N'', ThuTuHienThi=5, TrangThaiKichHoat=1 WHERE MaBanner = 1006;

IF NOT EXISTS (SELECT 1 FROM QuangCaoBanner WHERE MaBanner = 2001)
BEGIN
    SET IDENTITY_INSERT QuangCaoBanner ON;
    INSERT INTO QuangCaoBanner (MaBanner,DuongDanAnh,DuongDanLienKet,ThuTuHienThi,MaQuanLyCapNhat,TrangThaiKichHoat) VALUES (2001,N'/uploads/admin/d5ca14e9-15e2-431f-950c-4acb33fd7c05.jpg',N'',6,N'Ngttu2006@gmail.com',1);
    SET IDENTITY_INSERT QuangCaoBanner OFF;
END
ELSE
    UPDATE QuangCaoBanner SET DuongDanLienKet=N'', ThuTuHienThi=6, TrangThaiKichHoat=1 WHERE MaBanner = 2001;


GO

-- ==================== 8. KÊNH TƯ VẤN (UPSERT) ====================
IF NOT EXISTS (SELECT 1 FROM KenhTuVan WHERE MaKenh = 1)
BEGIN
    SET IDENTITY_INSERT KenhTuVan ON;
    INSERT INTO KenhTuVan (MaKenh,UrlMessenger,UrlZalo,UrlSMS) VALUES (1,N'https://m.me/carshop',N'https://zalo.me/carshop',N'0906123456');
    SET IDENTITY_INSERT KenhTuVan OFF;
END
ELSE
    UPDATE KenhTuVan SET UrlMessenger=N'https://m.me/carshop', UrlZalo=N'https://zalo.me/carshop', UrlSMS=N'0906123456' WHERE MaKenh = 1;
GO

-- ==================== 9. TỒN KHO THEO CHI NHÁNH (MERGE) + ĐỒNG BỘ PHIÊN BẢN ====================
MERGE INTO TonKhoTheoChiNhanh AS T
USING (VALUES
 (1,N'CN01',2), (1,N'CN02',1), (1,N'CN03',3), (1,N'CN04',2), (1,N'CN05',1), (1,N'CN06',1), (2,N'CN01',1), (2,N'CN02',1), (2,N'CN03',3), (2,N'CN04',3), (2,N'CN06',0), (3,N'CN01',1), (3,N'CN02',1), (3,N'CN03',3), (3,N'CN04',3), (3,N'CN06',0), (4,N'CN01',1), (4,N'CN02',0), (4,N'CN03',2), (4,N'CN04',3), (4,N'CN06',0), (5,N'CN01',2), (5,N'CN02',1), (5,N'CN03',3), (5,N'CN04',2), (5,N'CN05',1), (5,N'CN06',1), (6,N'CN01',1), (6,N'CN03',2), (6,N'CN04',3), (6,N'CN06',0), (7,N'CN01',1), (7,N'CN02',1), (7,N'CN03',3), (7,N'CN04',3), (7,N'CN06',0), (8,N'CN01',1), (8,N'CN03',2), (8,N'CN04',3), (8,N'CN06',0), (9,N'CN01',2), (9,N'CN02',2), (9,N'CN03',4), (9,N'CN04',4), (9,N'CN05',1), (9,N'CN06',1), (10,N'CN01',2), (10,N'CN02',1), (10,N'CN03',3), (10,N'CN04',4), (10,N'CN05',1), (10,N'CN06',1), (11,N'CN01',1), (11,N'CN02',1), (11,N'CN03',3), (11,N'CN04',3), (11,N'CN06',0), (12,N'CN01',1), (12,N'CN03',2), (12,N'CN04',3), (12,N'CN06',0), (13,N'CN01',1), (13,N'CN02',1), (13,N'CN03',3), (13,N'CN04',2), (13,N'CN06',0), (14,N'CN01',1), (14,N'CN03',2), (14,N'CN04',3), (14,N'CN06',0), (15,N'CN01',1), (15,N'CN02',0), (15,N'CN03',2), (15,N'CN04',3), (15,N'CN06',0), (16,N'CN03',2), (16,N'CN04',2), (16,N'CN06',0), (17,N'CN01',1), (17,N'CN02',0), (17,N'CN03',2), (17,N'CN04',3), (17,N'CN06',0), (18,N'CN03',1), (18,N'CN04',2), (18,N'CN06',0), (19,N'CN01',1), (19,N'CN02',1), (19,N'CN03',3), (19,N'CN04',2), (19,N'CN06',0), (20,N'CN01',1), (20,N'CN03',2), (20,N'CN04',3), (20,N'CN06',0), (21,N'CN01',2), (21,N'CN02',1), (21,N'CN03',3), (21,N'CN04',2), (21,N'CN05',1), (21,N'CN06',1), (22,N'CN01',1), (22,N'CN02',1), (22,N'CN03',3), (22,N'CN04',2), (22,N'CN06',0), (23,N'CN01',1), (23,N'CN02',0), (23,N'CN03',2), (23,N'CN04',3), (23,N'CN06',0), (25,N'CN01',1), (25,N'CN03',2), (25,N'CN04',2), (25,N'CN06',0), (26,N'CN03',1), (26,N'CN04',2), (26,N'CN06',0), (27,N'CN03',1), (27,N'CN04',2), (27,N'CN06',0), (28,N'CN01',1), (28,N'CN03',2), (28,N'CN04',2), (28,N'CN06',0), (29,N'CN03',1), (29,N'CN04',1), (29,N'CN06',0), (30,N'CN06',0), (31,N'CN01',1), (31,N'CN02',1), (31,N'CN03',3), (31,N'CN04',3), (31,N'CN06',0), (32,N'CN01',1), (32,N'CN03',2), (32,N'CN04',3), (32,N'CN06',0), (33,N'CN01',2), (33,N'CN02',1), (33,N'CN03',3), (33,N'CN04',4), (33,N'CN05',1), (33,N'CN06',1), (34,N'CN01',2), (34,N'CN02',1), (34,N'CN03',3), (34,N'CN04',2), (34,N'CN05',1), (34,N'CN06',1), (35,N'CN01',1), (35,N'CN02',0), (35,N'CN03',2), (35,N'CN04',3), (35,N'CN06',0), (36,N'CN01',1), (36,N'CN03',2), (36,N'CN04',2), (36,N'CN06',0), (37,N'CN01',1), (37,N'CN02',1), (37,N'CN03',3), (37,N'CN04',2), (37,N'CN06',0), (38,N'CN01',1), (38,N'CN03',2), (38,N'CN04',3), (38,N'CN06',0), (39,N'CN01',1), (39,N'CN03',2), (39,N'CN04',2), (39,N'CN06',0), (40,N'CN03',1), (40,N'CN04',2), (40,N'CN06',0), (41,N'CN01',1), (41,N'CN03',2), (41,N'CN04',3), (41,N'CN06',0), (42,N'CN01',1), (42,N'CN03',2), (42,N'CN04',2), (42,N'CN06',0), (43,N'CN01',1), (43,N'CN03',2), (43,N'CN04',2), (43,N'CN06',0), (44,N'CN03',1), (44,N'CN04',1), (44,N'CN06',0), (45,N'CN06',0), (46,N'CN01',1), (46,N'CN03',2), (46,N'CN04',2), (46,N'CN06',0), (47,N'CN03',1), (47,N'CN04',2), (47,N'CN06',0), (48,N'CN03',1), (48,N'CN04',1), (48,N'CN06',0), (49,N'CN03',1), (49,N'CN04',1), (49,N'CN06',0), (50,N'CN01',1), (50,N'CN03',2), (50,N'CN04',3), (50,N'CN06',0), (51,N'CN01',1), (51,N'CN03',2), (51,N'CN04',2), (51,N'CN06',0), (52,N'CN03',1), (52,N'CN04',2), (52,N'CN06',0), (53,N'CN01',1), (53,N'CN03',2), (53,N'CN04',3), (53,N'CN06',0), (54,N'CN06',0), (55,N'CN01',1), (55,N'CN03',2), (55,N'CN04',2), (55,N'CN06',0), (56,N'CN03',1), (56,N'CN04',1), (56,N'CN06',0), (57,N'CN01',1), (57,N'CN03',2), (57,N'CN04',2), (57,N'CN06',0), (58,N'CN03',1), (58,N'CN04',1), (58,N'CN06',0), (59,N'CN01',1), (59,N'CN02',1), (59,N'CN03',3), (59,N'CN04',2), (59,N'CN06',0), (60,N'CN01',1), (60,N'CN03',2), (60,N'CN04',2), (60,N'CN06',0), (61,N'CN01',1), (61,N'CN02',1), (61,N'CN03',3), (61,N'CN04',4), (61,N'CN06',0), (62,N'CN01',1), (62,N'CN02',0), (62,N'CN03',2), (62,N'CN04',3), (62,N'CN06',0), (63,N'CN01',2), (63,N'CN02',1), (63,N'CN03',3), (63,N'CN04',2), (63,N'CN05',1), (63,N'CN06',1), (64,N'CN01',1), (64,N'CN02',1), (64,N'CN03',3), (64,N'CN04',3), (64,N'CN06',0), (65,N'CN01',1), (65,N'CN02',1), (65,N'CN03',3), (65,N'CN04',2), (65,N'CN06',0), (66,N'CN01',1), (66,N'CN02',0), (66,N'CN03',2), (66,N'CN04',3), (66,N'CN06',0), (67,N'CN01',1), (67,N'CN03',2), (67,N'CN04',3), (67,N'CN06',0), (68,N'CN03',1), (68,N'CN04',2), (68,N'CN06',0), (69,N'CN01',1), (69,N'CN02',1), (69,N'CN03',3), (69,N'CN04',2), (69,N'CN06',0), (70,N'CN01',1), (70,N'CN03',2), (70,N'CN04',2), (70,N'CN06',0), (71,N'CN01',1), (71,N'CN02',1), (71,N'CN03',3), (71,N'CN04',3), (71,N'CN06',0), (72,N'CN01',1), (72,N'CN02',0), (72,N'CN03',2), (72,N'CN04',3), (72,N'CN06',0), (73,N'CN01',2), (73,N'CN02',1), (73,N'CN03',3), (73,N'CN04',4), (73,N'CN05',1), (73,N'CN06',1), (74,N'CN01',2), (74,N'CN02',1), (74,N'CN03',3), (74,N'CN04',2), (74,N'CN05',1), (74,N'CN06',1), (75,N'CN01',1), (75,N'CN02',1), (75,N'CN03',3), (75,N'CN04',3), (75,N'CN06',0), (76,N'CN01',1), (76,N'CN02',0), (76,N'CN03',2), (76,N'CN04',3), (76,N'CN06',0), (77,N'CN01',1), (77,N'CN03',2), (77,N'CN04',2), (77,N'CN06',0), (78,N'CN01',1), (78,N'CN02',1), (78,N'CN03',3), (78,N'CN04',4), (78,N'CN06',0), (79,N'CN01',1), (79,N'CN02',0), (79,N'CN03',2), (79,N'CN04',3), (79,N'CN06',0), (80,N'CN01',1), (80,N'CN02',0), (80,N'CN03',2), (80,N'CN04',3), (80,N'CN06',0), (81,N'CN03',1), (81,N'CN04',2), (81,N'CN06',0), (82,N'CN01',1), (82,N'CN02',1), (82,N'CN03',3), (82,N'CN04',2), (82,N'CN06',0), (83,N'CN01',1), (83,N'CN02',0), (83,N'CN03',2), (83,N'CN04',3), (83,N'CN06',0), (84,N'CN01',1), (84,N'CN02',1), (84,N'CN03',3), (84,N'CN04',2), (84,N'CN06',0), (85,N'CN01',1), (85,N'CN02',1), (85,N'CN03',3), (85,N'CN04',3), (85,N'CN06',0), (86,N'CN01',1), (86,N'CN03',2), (86,N'CN04',2), (86,N'CN06',0), (87,N'CN01',1), (87,N'CN02',1), (87,N'CN03',3), (87,N'CN04',2), (87,N'CN06',0), (88,N'CN01',1), (88,N'CN02',0), (88,N'CN03',2), (88,N'CN04',2), (88,N'CN06',0), (89,N'CN01',1), (89,N'CN02',0), (89,N'CN03',2), (89,N'CN04',3), (89,N'CN06',0), (90,N'CN01',1), (90,N'CN03',2), (90,N'CN04',2), (90,N'CN06',0), (91,N'CN01',1), (91,N'CN02',0), (91,N'CN03',2), (91,N'CN04',3), (91,N'CN06',0), (92,N'CN01',1), (92,N'CN02',1), (92,N'CN03',3), (92,N'CN04',2), (92,N'CN06',0), (93,N'CN01',1), (93,N'CN03',2), (93,N'CN04',2), (93,N'CN06',0), (94,N'CN03',1), (94,N'CN04',2), (94,N'CN06',0), (95,N'CN03',1), (95,N'CN04',1), (95,N'CN06',0), (96,N'CN03',1), (96,N'CN04',1), (96,N'CN06',0)
) AS S(MaPhienBan,MaChiNhanh,SoLuong)
ON T.MaPhienBan = S.MaPhienBan AND T.MaChiNhanh = S.MaChiNhanh
WHEN MATCHED THEN UPDATE SET SoLuong=S.SoLuong, NgayCapNhat=GETDATE()
WHEN NOT MATCHED THEN INSERT (MaPhienBan,MaChiNhanh,SoLuong,NgayCapNhat) VALUES (S.MaPhienBan,S.MaChiNhanh,S.SoLuong,GETDATE());

UPDATE p SET
    p.SoLuongTrongKho = a.s,
    p.TrangThai = CASE WHEN a.s = 0 THEN N'Hết hàng' WHEN a.s <= 5 THEN N'Sắp hết' ELSE N'Còn hàng' END
FROM PhienBanXe_SanPham p
JOIN (SELECT MaPhienBan, SUM(SoLuong) s FROM TonKhoTheoChiNhanh GROUP BY MaPhienBan) a
  ON a.MaPhienBan = p.MaPhienBan;
GO

-- ==================== 10. CẬP NHẬT MẬT KHẨU (PBKDF2) ====================
UPDATE TaiKhoan SET MatKhau = N'vmkvfKErSh20Mgk5RAFnXA==.cpvftbad6YQAJyb4tnvFXlt0cVJnLzEufbEDLpv4XvA=' WHERE TenDangNhap = N'fntzzs682@gmail.com';
UPDATE TaiKhoan SET MatKhau = N'Y6c07iVOADL1QREtk9ICoA==.uYBbMN2BI+5id0mITtfxzQ7qJR487SxvnjhfKpQCWTA=' WHERE TenDangNhap = N'minhquanmkp123@gmail.com';
UPDATE TaiKhoan SET MatKhau = N'VXiDw90yeiJX4xbvy8InFw==.AhO+0sz6u9keAaY4KqoeFCfMmoo9eN/xG7xPBGcpOv4=' WHERE TenDangNhap = N'Ngttu2006@gmail.com';
UPDATE TaiKhoan SET MatKhau = N'4DU4sEyLcRCLWX0vIpXxJg==.+fXasslu3CZmZtAJi+U110CPQKU2dbUBB4OUKxHZj2g=' WHERE TenDangNhap = N'QuanlyCS1';
UPDATE TaiKhoan SET MatKhau = N'ThwDARdiyQS7W/7MMrbmlQ==.WpTtXOgkUGLiWJkxYgbZFDBM4nAInhVzxIDJPVHb93s=' WHERE TenDangNhap = N'QuanlyCS2';
UPDATE TaiKhoan SET MatKhau = N'5qT9B1D94Yz/9audhLhPxA==.piwvwCnjJtqj3Obai31WLUnsQaAWHdSYHOD+THOzexE=' WHERE TenDangNhap = N'QuanlyCS3';
UPDATE TaiKhoan SET MatKhau = N'smf2YkvpetqaLwWd4dcprw==.GnP0TPr4mSCkfX1vD5bCit7tn8ejljC7IzE/XGrgX0A=' WHERE TenDangNhap = N'QuanlyCS4';
UPDATE TaiKhoan SET MatKhau = N'nOhqvRmr8cBnHyIummlhng==.AFbaAqdHTugwhsTsmyR2eImD394d/94+VbRP5dpOTQo=' WHERE TenDangNhap = N'QuanlyCS5';
UPDATE TaiKhoan SET MatKhau = N'plVucQYEuBjLD7tICOBPsA==.YQQ77Aw2dPIADfyV88SXO0/9fEIDy9+ozZbfHY+NYiM=' WHERE TenDangNhap = N'QuanlyCS6';
UPDATE TaiKhoan SET MatKhau = N'mONfji6a2TCRf2SkxVC4XA==.m2xfTGs9IxY/zA48jYvMeQyvQLNV3ojEPsCVFR/9G2g=' WHERE TenDangNhap = N'thanhdac223@gmail.com';
UPDATE TaiKhoan SET MatKhau = N'RrGdx0932vTBbe7TtC7XMg==.z1yGHr6z9T7jW3le5pODP7+8qwwKY8fvoMh/63D/Qgg=' WHERE TenDangNhap = N'user1';
UPDATE TaiKhoan SET MatKhau = N'D+gvKgilUrYNxWSGjLBiiA==.FejuI2thkV/i8d+2CnK2Kf9o2eJ+KaZqfHNU88L/Rao=' WHERE TenDangNhap = N'user2';
UPDATE TaiKhoan SET MatKhau = N'FGAO0dhh4hpX6IXg7UP7Xg==.VgoHnnaKcU+96KaRIw/fEIzJvxXktpfS3KwrVPg9y2E=' WHERE TenDangNhap = N'user3';
UPDATE TaiKhoan SET MatKhau = N'pqJk4eU0McL3ZIbXAs5Htw==.n2zLZfvFFjRXMpCmA4DN2kYmITsC1HhPZk8oiH6zi/k=' WHERE TenDangNhap = N'user4';
UPDATE TaiKhoan SET MatKhau = N'JkczGiUWrJPGO1KiE8kwBw==.uHc6YGr9tVyXdVOQlt47dXLyo+7vzOMZCZRAGv5MgMc=' WHERE TenDangNhap = N'user5';
UPDATE TaiKhoan SET MatKhau = N'cG2v6iFxx5mDaZKKjeQ2vw==.ExgqY3iSU1Nl4zPNCgItChA1pWJX/XzHfOC/dKFXNGw=' WHERE TenDangNhap = N'Vanh280306@gmail.com';
GO

-- ==================== 11. XEM LẠI DỮ LIỆU ====================
PRINT N'=== SỐ LƯỢNG ===';
SELECT 'HangXe' AS Bang, COUNT(*) AS SoLuong FROM HangXe
UNION ALL SELECT 'DongXe', COUNT(*) FROM DongXe
UNION ALL SELECT 'PhienBanXe_SanPham', COUNT(*) FROM PhienBanXe_SanPham
UNION ALL SELECT 'HinhAnhXe', COUNT(*) FROM HinhAnhXe
UNION ALL SELECT 'TonKhoTheoChiNhanh', COUNT(*) FROM TonKhoTheoChiNhanh
UNION ALL SELECT 'ChiNhanhShowroom', COUNT(*) FROM ChiNhanhShowroom
ORDER BY Bang;
-- Kiểm tra tổng tồn kho khớp SoLuongTrongKho (phải trả về 0 dòng)
SELECT a.MaPhienBan FROM (SELECT MaPhienBan, SUM(SoLuong) s FROM TonKhoTheoChiNhanh GROUP BY MaPhienBan) a
JOIN PhienBanXe_SanPham p ON p.MaPhienBan = a.MaPhienBan WHERE a.s <> p.SoLuongTrongKho;