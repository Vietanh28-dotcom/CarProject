using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuongDanAnh",
                table: "DongXe",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDich",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SepayTransactionId",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GioHang",
                columns: table => new
                {
                    MaGioHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    MaPhienBan = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHang", x => x.MaGioHang);
                    table.ForeignKey(
                        name: "FK_GioHang_PhienBanXe_SanPham_MaPhienBan",
                        column: x => x.MaPhienBan,
                        principalTable: "PhienBanXe_SanPham",
                        principalColumn: "MaPhienBan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GioHang_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "TenDangNhap");
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiNhan = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaXem = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_TaiKhoan_MaNguoiNhan",
                        column: x => x.MaNguoiNhan,
                        principalTable: "TaiKhoan",
                        principalColumn: "TenDangNhap");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_MaPhienBan",
                table: "GioHang",
                column: "MaPhienBan");

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_MaTaiKhoan",
                table: "GioHang",
                column: "MaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaNguoiNhan",
                table: "ThongBao",
                column: "MaNguoiNhan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GioHang");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropColumn(
                name: "DuongDanAnh",
                table: "DongXe");

            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "MaGiaoDich",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "SepayTransactionId",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "DonDatCoc");
        }
    }
}
