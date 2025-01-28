using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class addedComputer_IdsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "computer_ids",
                table: "user_accountability_lists",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UserAccountabilityListid",
                table: "computers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_computers_UserAccountabilityListid",
                table: "computers",
                column: "UserAccountabilityListid");

            migrationBuilder.AddForeignKey(
                name: "FK_computers_user_accountability_lists_UserAccountabilityListid",
                table: "computers",
                column: "UserAccountabilityListid",
                principalTable: "user_accountability_lists",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_computers_user_accountability_lists_UserAccountabilityListid",
                table: "computers");

            migrationBuilder.DropIndex(
                name: "IX_computers_UserAccountabilityListid",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "computer_ids",
                table: "user_accountability_lists");

            migrationBuilder.DropColumn(
                name: "UserAccountabilityListid",
                table: "computers");
        }
    }
}
