using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddYeuCauDieuChuyenAndCanNhapHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanNhapHang",
                table: "DonDatCocChiTiet",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "YeuCauDieuChuyenXe",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonCoc = table.Column<int>(type: "int", nullable: false),
                    MaChiTiet = table.Column<int>(type: "int", nullable: false),
                    MaPhienBan = table.Column<int>(type: "int", nullable: false),
                    MaChiNhanhNguon = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MaChiNhanhDich = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiGui = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiPhanHoi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayPhanHoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauDieuChuyenXe", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauDieuChuyenXe_ChiNhanhShowroom_MaChiNhanhDich",
                        column: x => x.MaChiNhanhDich,
                        principalTable: "ChiNhanhShowroom",
                        principalColumn: "MaChiNhanh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDieuChuyenXe_ChiNhanhShowroom_MaChiNhanhNguon",
                        column: x => x.MaChiNhanhNguon,
                        principalTable: "ChiNhanhShowroom",
                        principalColumn: "MaChiNhanh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDieuChuyenXe_DonDatCocChiTiet_MaChiTiet",
                        column: x => x.MaChiTiet,
                        principalTable: "DonDatCocChiTiet",
                        principalColumn: "MaChiTiet",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDieuChuyenXe_DonDatCoc_MaDonCoc",
                        column: x => x.MaDonCoc,
                        principalTable: "DonDatCoc",
                        principalColumn: "MaDonCoc",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_YeuCauDieuChuyenXe_PhienBanXe_SanPham_MaPhienBan",
                        column: x => x.MaPhienBan,
                        principalTable: "PhienBanXe_SanPham",
                        principalColumn: "MaPhienBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDieuChuyenXe_MaChiNhanhDich",
                table: "YeuCauDieuChuyenXe",
                column: "MaChiNhanhDich");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDieuChuyenXe_MaChiNhanhNguon",
                table: "YeuCauDieuChuyenXe",
                column: "MaChiNhanhNguon");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDieuChuyenXe_MaChiTiet",
                table: "YeuCauDieuChuyenXe",
                column: "MaChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDieuChuyenXe_MaDonCoc",
                table: "YeuCauDieuChuyenXe",
                column: "MaDonCoc");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDieuChuyenXe_MaPhienBan",
                table: "YeuCauDieuChuyenXe",
                column: "MaPhienBan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauDieuChuyenXe");

            migrationBuilder.DropColumn(
                name: "CanNhapHang",
                table: "DonDatCocChiTiet");
        }
    }
}
