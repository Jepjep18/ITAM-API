using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT_ASSET.Migrations
{
    /// <inheritdoc />
    public partial class modifyModelToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Users_OwnerId",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Users",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "Users",
                newName: "department");

            migrationBuilder.RenameColumn(
                name: "Company",
                table: "Users",
                newName: "company");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Warranty",
                table: "Assets",
                newName: "warranty");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Assets",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Storage",
                table: "Assets",
                newName: "storage");

            migrationBuilder.RenameColumn(
                name: "Size",
                table: "Assets",
                newName: "size");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "Assets",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "RAM",
                table: "Assets",
                newName: "ram");

            migrationBuilder.RenameColumn(
                name: "PO",
                table: "Assets",
                newName: "po");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "Assets",
                newName: "model");

            migrationBuilder.RenameColumn(
                name: "History",
                table: "Assets",
                newName: "history");

            migrationBuilder.RenameColumn(
                name: "GPU",
                table: "Assets",
                newName: "gpu");

            migrationBuilder.RenameColumn(
                name: "Cost",
                table: "Assets",
                newName: "cost");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "Assets",
                newName: "color");

            migrationBuilder.RenameColumn(
                name: "Brand",
                table: "Assets",
                newName: "brand");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Assets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SerialNo",
                table: "Assets",
                newName: "serial_no");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Assets",
                newName: "owner_id");

            migrationBuilder.RenameColumn(
                name: "LiDescription",
                table: "Assets",
                newName: "li_description");

            migrationBuilder.RenameColumn(
                name: "AssetBarcode",
                table: "Assets",
                newName: "asset_barcode");

            migrationBuilder.RenameColumn(
                name: "AcquisitionDate",
                table: "Assets",
                newName: "acquisition_date");

            migrationBuilder.RenameIndex(
                name: "IX_Assets_OwnerId",
                table: "Assets",
                newName: "IX_Assets_owner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Users_owner_id",
                table: "Assets",
                column: "owner_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Users_owner_id",
                table: "Assets");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "department",
                table: "Users",
                newName: "Department");

            migrationBuilder.RenameColumn(
                name: "company",
                table: "Users",
                newName: "Company");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "warranty",
                table: "Assets",
                newName: "Warranty");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Assets",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "storage",
                table: "Assets",
                newName: "Storage");

            migrationBuilder.RenameColumn(
                name: "size",
                table: "Assets",
                newName: "Size");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "Assets",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "ram",
                table: "Assets",
                newName: "RAM");

            migrationBuilder.RenameColumn(
                name: "po",
                table: "Assets",
                newName: "PO");

            migrationBuilder.RenameColumn(
                name: "model",
                table: "Assets",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "history",
                table: "Assets",
                newName: "History");

            migrationBuilder.RenameColumn(
                name: "gpu",
                table: "Assets",
                newName: "GPU");

            migrationBuilder.RenameColumn(
                name: "cost",
                table: "Assets",
                newName: "Cost");

            migrationBuilder.RenameColumn(
                name: "color",
                table: "Assets",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "brand",
                table: "Assets",
                newName: "Brand");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Assets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "serial_no",
                table: "Assets",
                newName: "SerialNo");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "Assets",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "li_description",
                table: "Assets",
                newName: "LiDescription");

            migrationBuilder.RenameColumn(
                name: "asset_barcode",
                table: "Assets",
                newName: "AssetBarcode");

            migrationBuilder.RenameColumn(
                name: "acquisition_date",
                table: "Assets",
                newName: "AcquisitionDate");

            migrationBuilder.RenameIndex(
                name: "IX_Assets_owner_id",
                table: "Assets",
                newName: "IX_Assets_OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Users_OwnerId",
                table: "Assets",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
