using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class addedRelationshipforComputerandUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_computers_Users_ownerid",
                table: "computers");

            migrationBuilder.DropIndex(
                name: "IX_computers_ownerid",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "ownerid",
                table: "computers");

            migrationBuilder.CreateIndex(
                name: "IX_computers_owner_id",
                table: "computers",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_computers_Users_owner_id",
                table: "computers",
                column: "owner_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_computers_Users_owner_id",
                table: "computers");

            migrationBuilder.DropIndex(
                name: "IX_computers_owner_id",
                table: "computers");

            migrationBuilder.AddColumn<int>(
                name: "ownerid",
                table: "computers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_computers_ownerid",
                table: "computers",
                column: "ownerid");

            migrationBuilder.AddForeignKey(
                name: "FK_computers_Users_ownerid",
                table: "computers",
                column: "ownerid",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
