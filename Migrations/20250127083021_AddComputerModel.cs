using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class AddComputerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "computers",
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
                    ownerid = table.Column<int>(type: "int", nullable: false),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    date_modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_computers", x => x.id);
                    table.ForeignKey(
                        name: "FK_computers_Users_ownerid",
                        column: x => x.ownerid,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_computers_ownerid",
                table: "computers",
                column: "ownerid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "computers");
        }
    }
}
