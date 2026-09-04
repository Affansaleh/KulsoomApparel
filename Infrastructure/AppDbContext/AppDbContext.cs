using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.AppDbContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<StitchingTeam> StitchingTeams => Set<StitchingTeam>();
    public DbSet<Fabric> Fabrics => Set<Fabric>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleFabric> ArticleFabrics => Set<ArticleFabric>();
    public DbSet<ArticleAlternateCode> ArticleAlternateCodes => Set<ArticleAlternateCode>();
    public DbSet<ArticleSizeBreakdown> ArticleSizeBreakdowns => Set<ArticleSizeBreakdown>();
    public DbSet<ArticleCuttingSizeBreakdown> ArticleCuttingSizeBreakdowns => Set<ArticleCuttingSizeBreakdown>();
    public DbSet<ArticleDepartmentStatus> ArticleDepartmentStatuses => Set<ArticleDepartmentStatus>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<StatusLog> StatusLogs => Set<StatusLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------- User ----------------
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

            e.HasOne(u => u.Department)
             .WithMany(d => d.Managers)
             .HasForeignKey(u => u.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordResetOtp>(e =>
        {
            e.Property(o => o.OtpCode).HasMaxLength(6).IsRequired();

            e.HasOne(o => o.User)
             .WithMany()
             .HasForeignKey(o => o.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Department ----------------
        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(d => d.Name).IsUnique();
            e.Property(d => d.Name).HasMaxLength(50).IsRequired();
        });

        // ---------------- StitchingTeam ----------------
        modelBuilder.Entity<StitchingTeam>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(50).IsRequired();

            e.HasOne(t => t.Department)
             .WithMany(d => d.Teams)
             .HasForeignKey(t => t.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Fabric ----------------
        modelBuilder.Entity<Fabric>(e =>
        {
            e.HasIndex(f => new { f.FabricCode, f.Color }).IsUnique();
            e.Property(f => f.FabricCode).HasMaxLength(50).IsRequired();
            e.Property(f => f.FabricType).HasMaxLength(50).IsRequired();
            e.Property(f => f.Color).HasMaxLength(50).IsRequired();

            e.Property(f => f.Quantity).HasPrecision(18, 2);
            e.Property(f => f.AvailableQuantity).HasPrecision(18, 2);
            e.Property(f => f.Rate).HasPrecision(18, 2);
            e.Property(f => f.TotalAmount).HasPrecision(18, 2);
        });

        // ---------------- Article ----------------
        modelBuilder.Entity<Article>(e =>
        {
            e.HasIndex(a => a.ArticleCode).IsUnique();
            e.Property(a => a.ArticleCode).HasMaxLength(50).IsRequired();
            e.Property(a => a.CompanyName).HasMaxLength(100).IsRequired();

            e.Property(a => a.PricePerPiece).HasPrecision(18, 2);
            e.Property(a => a.PriceTotal).HasPrecision(18, 2);

            e.HasOne(a => a.CreatedBy)
             .WithMany()
             .HasForeignKey(a => a.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.AssignedTeam)
             .WithMany()
             .HasForeignKey(a => a.AssignedTeamId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ---------------- ArticleFabric ----------------
        modelBuilder.Entity<ArticleFabric>(e =>
        {
            e.Property(af => af.QuantityUsed).HasPrecision(18, 2);

            e.HasOne(af => af.Article)
             .WithMany(a => a.FabricLinks)
             .HasForeignKey(af => af.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(af => af.Fabric)
             .WithMany(f => f.ArticleLinks)
             .HasForeignKey(af => af.FabricId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- ArticleAlternateCode ----------------
        modelBuilder.Entity<ArticleAlternateCode>(e =>
        {
            e.HasIndex(ac => ac.Code).IsUnique();
            e.Property(ac => ac.Code).HasMaxLength(50).IsRequired();

            e.HasOne(ac => ac.Article)
             .WithMany(a => a.AlternateCodes)
             .HasForeignKey(ac => ac.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- ArticleSizeBreakdown ----------------
        modelBuilder.Entity<ArticleSizeBreakdown>(e =>
        {
            e.Property(sb => sb.SizeLabel).HasMaxLength(20).IsRequired();

            e.HasOne(sb => sb.Article)
             .WithMany(a => a.SizeBreakdowns)
             .HasForeignKey(sb => sb.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- ArticleCuttingSizeBreakdown ----------------
        modelBuilder.Entity<ArticleCuttingSizeBreakdown>(e =>
        {
            e.Property(sb => sb.SizeLabel).HasMaxLength(20).IsRequired();
            e.HasIndex(sb => new { sb.ArticleId, sb.SizeLabel }).IsUnique();
            e.HasOne(sb => sb.Article)
             .WithMany(a => a.CuttingSizeBreakdowns)
             .HasForeignKey(sb => sb.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- ArticleDepartmentStatus ----------------
        modelBuilder.Entity<ArticleDepartmentStatus>(e =>
        {
            e.HasOne(ads => ads.Article)
             .WithMany(a => a.DepartmentStatuses)
             .HasForeignKey(ads => ads.ArticleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ads => ads.Department)
             .WithMany(d => d.ArticleStatuses)
             .HasForeignKey(ads => ads.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(ads => ads.UpdatedBy)
             .WithMany()
             .HasForeignKey(ads => ads.UpdatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.Property(ads => ads.SamplingApprovalState).HasMaxLength(30);
            e.Property(ads => ads.SamplingReviewNote).HasMaxLength(1000);

            e.HasIndex(ads => new { ads.ArticleId, ads.DepartmentId }).IsUnique();
        });

        // ---------------- StatusLog ----------------
        modelBuilder.Entity<StatusLog>(e =>
        {
            e.HasOne(sl => sl.ArticleDepartmentStatus)
             .WithMany()
             .HasForeignKey(sl => sl.ArticleDepartmentStatusId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(sl => sl.ChangedBy)
             .WithMany()
             .HasForeignKey(sl => sl.ChangedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });


    }
}