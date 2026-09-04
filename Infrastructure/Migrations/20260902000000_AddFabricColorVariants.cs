using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260902000000_AddFabricColorVariants")]
public partial class AddFabricColorVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Fabrics_FabricCode", table: "Fabrics");
        migrationBuilder.AddColumn<string>(
            name: "Color", table: "Fabrics", type: "nvarchar(50)", maxLength: 50,
            nullable: false, defaultValue: "Unspecified");
        migrationBuilder.CreateIndex(
            name: "IX_Fabrics_FabricCode_Color", table: "Fabrics",
            columns: new[] { "FabricCode", "Color" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Fabrics_FabricCode_Color", table: "Fabrics");
        migrationBuilder.DropColumn(name: "Color", table: "Fabrics");
        migrationBuilder.CreateIndex(name: "IX_Fabrics_FabricCode", table: "Fabrics", column: "FabricCode", unique: true);
    }
}
