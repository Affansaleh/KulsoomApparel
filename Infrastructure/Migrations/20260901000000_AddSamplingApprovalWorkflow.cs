using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260901000000_AddSamplingApprovalWorkflow")]
public partial class AddSamplingApprovalWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "SamplingAttemptCount", table: "ArticleDepartmentStatuses", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "SamplingApprovalState", table: "ArticleDepartmentStatuses", type: "nvarchar(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SamplingSubmittedAt", table: "ArticleDepartmentStatuses", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SamplingReviewedAt", table: "ArticleDepartmentStatuses", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(name: "SamplingReviewedByUserId", table: "ArticleDepartmentStatuses", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SamplingReviewNote", table: "ArticleDepartmentStatuses", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SamplingAttemptCount", table: "ArticleDepartmentStatuses");
        migrationBuilder.DropColumn(name: "SamplingApprovalState", table: "ArticleDepartmentStatuses");
        migrationBuilder.DropColumn(name: "SamplingSubmittedAt", table: "ArticleDepartmentStatuses");
        migrationBuilder.DropColumn(name: "SamplingReviewedAt", table: "ArticleDepartmentStatuses");
        migrationBuilder.DropColumn(name: "SamplingReviewedByUserId", table: "ArticleDepartmentStatuses");
        migrationBuilder.DropColumn(name: "SamplingReviewNote", table: "ArticleDepartmentStatuses");
    }
}
