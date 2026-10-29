using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingFieldsAndGioHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MaQuanLyDuyet",
                table: "DonDatCoc",
                type: "nvarchar(100)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");

            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DiaChi",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_MaPhienBan",
                table: "GioHang",
                column: "MaPhienBan");

            migrationBuilder.CreateIndex(
                name: "IX_GioHang_MaTaiKhoan",
                table: "GioHang",
                column: "MaTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GioHang");

            migrationBuilder.DropColumn(
                name: "DiaChi",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "DonDatCoc");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "DonDatCoc");

            migrationBuilder.AlterColumn<string>(
                name: "MaQuanLyDuyet",
                table: "DonDatCoc",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GhiChu",
                table: "DonDatCoc",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
