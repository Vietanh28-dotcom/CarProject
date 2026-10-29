using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddHinhAnhXe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HinhAnhXe",
                columns: table => new
                {
                    MaHinhAnh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDong = table.Column<int>(type: "int", nullable: false),
                    DuongDanAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LaChinh = table.Column<bool>(type: "bit", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HinhAnhXe", x => x.MaHinhAnh);
                    table.ForeignKey(
                        name: "FK_HinhAnhXe_DongXe_MaDong",
                        column: x => x.MaDong,
                        principalTable: "DongXe",
                        principalColumn: "MaDong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HinhAnhXe_MaDong",
                table: "HinhAnhXe",
                column: "MaDong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HinhAnhXe");
        }
    }
}
