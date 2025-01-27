using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    company = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    employee_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    e_signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "computer_components",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    asset_barcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    owner_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_computer_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_computer_components_Users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_accountability_lists",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    accountability_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    tracking_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    asset_ids = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accountability_lists", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_accountability_lists_Users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    performed_by_user_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_logs_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    date_acquired = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    asset_barcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    brand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ram = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ssd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    hdd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gpu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    size = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    serial_no = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    po = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warranty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    li_description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    history = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    asset_image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    owner_id = table.Column<int>(type: "int", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_modified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserAccountabilityListid = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_Assets_Users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_user_accountability_lists_UserAccountabilityListid",
                        column: x => x.UserAccountabilityListid,
                        principalTable: "user_accountability_lists",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "asset_Logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asset_id = table.Column<int>(type: "int", nullable: false),
                    action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    performed_by_user_id = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_Logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_Logs_Assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "Assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_Logs_asset_id",
                table: "asset_Logs",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_owner_id",
                table: "Assets",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserAccountabilityListid",
                table: "Assets",
                column: "UserAccountabilityListid");

            migrationBuilder.CreateIndex(
                name: "IX_computer_components_owner_id",
                table: "computer_components",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_accountability_lists_owner_id",
                table: "user_accountability_lists",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_logs_user_id",
                table: "user_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_Logs");

            migrationBuilder.DropTable(
                name: "computer_components");

            migrationBuilder.DropTable(
                name: "user_logs");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "user_accountability_lists");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
