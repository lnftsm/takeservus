using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TakeServus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitPriceToJobMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "JobMaterials",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "JobMaterials");
        }
    }
}
