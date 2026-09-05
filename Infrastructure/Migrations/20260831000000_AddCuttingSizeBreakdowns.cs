using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260831000000_AddCuttingSizeBreakdowns")]
public partial class AddCuttingSizeBreakdowns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ArticleCuttingSizeBreakdowns",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ArticleId = table.Column<int>(type: "int", nullable: false),
                SizeLabel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                OrderIndex = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArticleCuttingSizeBreakdowns", x => x.Id);
                table.ForeignKey(
                    name: "FK_ArticleCuttingSizeBreakdowns_Articles_ArticleId",
                    column: x => x.ArticleId,
                    principalTable: "Articles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ArticleCuttingSizeBreakdowns_ArticleId_SizeLabel",
            table: "ArticleCuttingSizeBreakdowns",
            columns: new[] { "ArticleId", "SizeLabel" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ArticleCuttingSizeBreakdowns");
    }
}
