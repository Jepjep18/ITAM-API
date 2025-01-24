using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMultiplehistoryColumnInAssetsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "History1",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "History2",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "History3",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "History4",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "History5",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "History6",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "History7",
                table: "Assets",
                newName: "History");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "History",
                table: "Assets",
                newName: "History7");

            migrationBuilder.AddColumn<string>(
                name: "History1",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "History2",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "History3",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "History4",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "History5",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "History6",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
