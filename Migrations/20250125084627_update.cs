using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_accountability_lists_Assets_asset_id",
                table: "user_accountability_lists");

            migrationBuilder.DropIndex(
                name: "IX_user_accountability_lists_asset_id",
                table: "user_accountability_lists");

            migrationBuilder.DropColumn(
                name: "asset_id",
                table: "user_accountability_lists");

            migrationBuilder.AddColumn<string>(
                name: "asset_ids",
                table: "user_accountability_lists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserAccountabilityListid",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserAccountabilityListid",
                table: "Assets",
                column: "UserAccountabilityListid");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_user_accountability_lists_UserAccountabilityListid",
                table: "Assets",
                column: "UserAccountabilityListid",
                principalTable: "user_accountability_lists",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_user_accountability_lists_UserAccountabilityListid",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_UserAccountabilityListid",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "asset_ids",
                table: "user_accountability_lists");

            migrationBuilder.DropColumn(
                name: "UserAccountabilityListid",
                table: "Assets");

            migrationBuilder.AddColumn<int>(
                name: "asset_id",
                table: "user_accountability_lists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_user_accountability_lists_asset_id",
                table: "user_accountability_lists",
                column: "asset_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_accountability_lists_Assets_asset_id",
                table: "user_accountability_lists",
                column: "asset_id",
                principalTable: "Assets",
                principalColumn: "id");
        }
    }
}
