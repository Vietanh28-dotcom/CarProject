using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDonDatCocChiTietTiepNhan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LyDoTuChoi",
                table: "DonDatCocChiTiet",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayPhanHoi",
                table: "DonDatCocChiTiet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiPhanHoi",
                table: "DonDatCocChiTiet",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiTiepNhan",
                table: "DonDatCocChiTiet",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Chờ xác nhận");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LyDoTuChoi",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "NgayPhanHoi",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "NguoiPhanHoi",
                table: "DonDatCocChiTiet");

            migrationBuilder.DropColumn(
                name: "TrangThaiTiepNhan",
                table: "DonDatCocChiTiet");
        }
    }
}
