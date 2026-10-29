using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDongXeAnhAndBannerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuongDanVideo",
                table: "QuangCaoBanner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiBanner",
                table: "QuangCaoBanner",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThoiGianChuyen",
                table: "QuangCaoBanner",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DuongDanAnh",
                table: "DongXe",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuongDanVideo",
                table: "QuangCaoBanner");

            migrationBuilder.DropColumn(
                name: "LoaiBanner",
                table: "QuangCaoBanner");

            migrationBuilder.DropColumn(
                name: "ThoiGianChuyen",
                table: "QuangCaoBanner");

            migrationBuilder.DropColumn(
                name: "DuongDanAnh",
                table: "DongXe");
        }
    }
}
