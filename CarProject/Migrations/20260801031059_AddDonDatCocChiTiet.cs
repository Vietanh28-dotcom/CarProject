using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDonDatCocChiTiet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaPhienBan",
                table: "DonDatCoc",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "DonDatCocChiTiet",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonCoc = table.Column<int>(type: "int", nullable: false),
                    MaPhienBan = table.Column<int>(type: "int", nullable: false),
                    MaChiNhanh = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonDatCocChiTiet", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_DonDatCocChiTiet_ChiNhanhShowroom_MaChiNhanh",
                        column: x => x.MaChiNhanh,
                        principalTable: "ChiNhanhShowroom",
                        principalColumn: "MaChiNhanh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonDatCocChiTiet_DonDatCoc_MaDonCoc",
                        column: x => x.MaDonCoc,
                        principalTable: "DonDatCoc",
                        principalColumn: "MaDonCoc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonDatCocChiTiet_PhienBanXe_SanPham_MaPhienBan",
                        column: x => x.MaPhienBan,
                        principalTable: "PhienBanXe_SanPham",
                        principalColumn: "MaPhienBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonDatCocChiTiet_MaChiNhanh",
                table: "DonDatCocChiTiet",
                column: "MaChiNhanh");

            migrationBuilder.CreateIndex(
                name: "IX_DonDatCocChiTiet_MaDonCoc",
                table: "DonDatCocChiTiet",
                column: "MaDonCoc");

            migrationBuilder.CreateIndex(
                name: "IX_DonDatCocChiTiet_MaPhienBan",
                table: "DonDatCocChiTiet",
                column: "MaPhienBan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonDatCocChiTiet");

            migrationBuilder.AlterColumn<int>(
                name: "MaPhienBan",
                table: "DonDatCoc",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
