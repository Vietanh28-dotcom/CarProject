using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddTonKhoTheoChiNhanh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TonKhoTheoChiNhanh",
                columns: table => new
                {
                    MaTonKho = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhienBan = table.Column<int>(type: "int", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKhoTheoChiNhanh", x => x.MaTonKho);
                    table.ForeignKey(
                        name: "FK_TonKhoTheoChiNhanh_ChiNhanhShowroom_MaChiNhanh",
                        column: x => x.MaChiNhanh,
                        principalTable: "ChiNhanhShowroom",
                        principalColumn: "MaChiNhanh");
                    table.ForeignKey(
                        name: "FK_TonKhoTheoChiNhanh_PhienBanXe_SanPham_MaPhienBan",
                        column: x => x.MaPhienBan,
                        principalTable: "PhienBanXe_SanPham",
                        principalColumn: "MaPhienBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TonKhoTheoChiNhanh_MaChiNhanh",
                table: "TonKhoTheoChiNhanh",
                column: "MaChiNhanh");

            migrationBuilder.CreateIndex(
                name: "IX_TonKhoTheoChiNhanh_MaPhienBan",
                table: "TonKhoTheoChiNhanh",
                column: "MaPhienBan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TonKhoTheoChiNhanh");
        }
    }
}
