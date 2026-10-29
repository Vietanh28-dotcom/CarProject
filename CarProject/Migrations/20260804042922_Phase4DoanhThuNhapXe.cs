using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase4DoanhThuNhapXe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaNhapKho",
                table: "DonDatCocChiTiet",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SoLuongThieu",
                table: "DonDatCocChiTiet",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienCocPhanBo",
                table: "DonDatCocChiTiet",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "SoTienNhapMoiXe",
                table: "DonDatCocChiTiet",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "DaTinhDoanhThu",
                table: "DonDatCoc",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PhieuNhapXe",
                columns: table => new
                {
                    MaPhieuNhap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonCoc = table.Column<int>(type: "int", nullable: false),
                    MaPhienBan = table.Column<int>(type: "int", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoLuongNhap = table.Column<int>(type: "int", nullable: false),
                    SoTienNhapMoiXe = table.Column<long>(type: "bigint", nullable: false),
                    TongSoTienNhap = table.Column<long>(type: "bigint", nullable: false),
                    NguoiNhap = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayNhap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhapXe", x => x.MaPhieuNhap);
                    table.ForeignKey(
                        name: "FK_PhieuNhapXe_ChiNhanhShowroom_MaChiNhanh",
                        column: x => x.MaChiNhanh,
                        principalTable: "ChiNhanhShowroom",
                        principalColumn: "MaChiNhanh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhapXe_DonDatCoc_MaDonCoc",
                        column: x => x.MaDonCoc,
                        principalTable: "DonDatCoc",
                        principalColumn: "MaDonCoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhapXe_PhienBanXe_SanPham_MaPhienBan",
                        column: x => x.MaPhienBan,
                        principalTable: "PhienBanXe_SanPham",
                        principalColumn: "MaPhienBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapXe_MaChiNhanh",
                table: "PhieuNhapXe",
                column: "MaChiNhanh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapXe_MaDonCoc",
                table: "PhieuNhapXe",
                column: "MaDonCoc");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhapXe_MaPhienBan",
                table: "PhieuNhapXe",
                column: "MaPhienBan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhieuNhapXe");

            migrationBuilder.DropColumn(
                name: "DaNhapKho",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "SoLuongThieu",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "SoTienCocPhanBo",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "SoTienNhapMoiXe",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "DaTinhDoanhThu",
                table: "DonDatCoc");
        }
    }
}
