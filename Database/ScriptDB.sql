SET NOCOUNT ON;
IF DB_ID(N'CafePOSDB') IS NULL EXEC(N'CREATE DATABASE [CafePOSDB]');
GO
USE [CafePOSDB];
GO
IF OBJECT_ID(N'dbo.BanAn',N'U') IS NULL
CREATE TABLE dbo.BanAn(MaBan int IDENTITY PRIMARY KEY, TenBan nvarchar(50) NOT NULL, TrangThai nvarchar(20) NOT NULL DEFAULT N'Trống' CHECK(TrangThai IN(N'Trống',N'Có khách')));
IF OBJECT_ID(N'dbo.DanhMuc',N'U') IS NULL
CREATE TABLE dbo.DanhMuc(MaDanhMuc int IDENTITY PRIMARY KEY, TenDanhMuc nvarchar(100) NOT NULL);
IF OBJECT_ID(N'dbo.MonAn',N'U') IS NULL
CREATE TABLE dbo.MonAn(MaMon int IDENTITY PRIMARY KEY, TenMon nvarchar(100) NOT NULL CHECK(LEN(LTRIM(RTRIM(TenMon)))>0), MaDanhMuc int NOT NULL REFERENCES dbo.DanhMuc(MaDanhMuc), DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0));
IF OBJECT_ID(N'dbo.HoaDon',N'U') IS NULL
CREATE TABLE dbo.HoaDon(MaHoaDon int IDENTITY PRIMARY KEY, MaBan int NOT NULL REFERENCES dbo.BanAn(MaBan), GioVao datetime NOT NULL DEFAULT GETDATE(), GioRa datetime NULL, TongTien decimal(18,2) NOT NULL DEFAULT 0 CHECK(TongTien>=0), TrangThai bit NOT NULL DEFAULT 0);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name=N'UX_HoaDon_BanDangMo' AND object_id=OBJECT_ID(N'dbo.HoaDon'))
CREATE UNIQUE INDEX UX_HoaDon_BanDangMo ON dbo.HoaDon(MaBan) WHERE TrangThai=0;
IF OBJECT_ID(N'dbo.ChiTietHoaDon',N'U') IS NULL
CREATE TABLE dbo.ChiTietHoaDon(MaHoaDon int NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),MaMon int NOT NULL REFERENCES dbo.MonAn(MaMon),SoLuong int NOT NULL CHECK(SoLuong>0),DonGia decimal(18,2) NOT NULL CHECK(DonGia>=0),PRIMARY KEY(MaHoaDon,MaMon));
IF NOT EXISTS(SELECT 1 FROM dbo.BanAn) INSERT dbo.BanAn(TenBan) VALUES(N'Bàn 01'),(N'Bàn 02'),(N'Bàn 03'),(N'Bàn 04'),(N'Bàn 05'),(N'Bàn 06');
IF NOT EXISTS(SELECT 1 FROM dbo.DanhMuc) INSERT dbo.DanhMuc(TenDanhMuc) VALUES(N'Cà phê'),(N'Trà sữa'),(N'Đồ ăn nhẹ');
IF NOT EXISTS(SELECT 1 FROM dbo.MonAn)
INSERT dbo.MonAn(TenMon,MaDanhMuc,DonGia)
SELECT s.TenMon,d.MaDanhMuc,s.DonGia FROM(VALUES(N'Cà phê đen',N'Cà phê',25000),(N'Cà phê sữa',N'Cà phê',30000),(N'Trà sữa trân châu',N'Trà sữa',35000),(N'Bánh mì',N'Đồ ăn nhẹ',20000)) s(TenMon,TenDanhMuc,DonGia) JOIN dbo.DanhMuc d ON d.TenDanhMuc=s.TenDanhMuc;
GO
