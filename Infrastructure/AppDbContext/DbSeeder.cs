using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.AppDbContext;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Idempotent - safe to call every startup, only runs if Departments table is empty
        if (await context.Departments.AnyAsync())
            return;

        // ---------------- Departments (fixed, in workflow order) ----------------
        var departmentDefinitions = new (string Name, DepartmentType Type, int Order)[]
        {
            ("Pattern", DepartmentType.Pattern, 1),
            ("Sampling", DepartmentType.Sampling, 2),
            ("Cutting", DepartmentType.Cutting, 3),
            ("Printing", DepartmentType.Printing, 4),
            ("Embroidery", DepartmentType.Embroidery, 5),
            ("Stitching", DepartmentType.Stitching, 6),
            ("HandWork", DepartmentType.HandWork, 7),
            ("Quality & Packing", DepartmentType.QualityAndPacking, 8),
            ("Delivery", DepartmentType.Delivery, 9),
        };

        var departments = new List<Department>();
        foreach (var def in departmentDefinitions)
        {
            var dept = new Department
            {
                Name = def.Name,
                Type = def.Type,
                OrderIndex = def.Order,
                IsActive = true
            };
            departments.Add(dept);
        }

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();   // need department Ids before creating users/teams

        // ---------------- Stitching Teams (A / B) ----------------
        var stitchingDept = departments.First(d => d.Type == DepartmentType.Stitching);
        var teams = new List<StitchingTeam>
        {
            new StitchingTeam { DepartmentId = stitchingDept.Id, Name = "Team A", IsActive = true },
            new StitchingTeam { DepartmentId = stitchingDept.Id, Name = "Team B", IsActive = true },
        };
        await context.StitchingTeams.AddRangeAsync(teams);

        // ---------------- Users: 1 Admin + 9 Managers + 1 Reader ----------------
        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = UserRole.Admin,
                DepartmentId = null,
                IsActive = true,
            },
            new User
            {
                Username = "reader",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("reader123"),
                Role = UserRole.Reader,
                DepartmentId = null,
                IsActive = true,
                
            }
        };

        foreach (var dept in departments)
        {
            // username = department name, lowercased, spaces/symbols removed
            var username = new string(dept.Name.ToLower().Where(char.IsLetterOrDigit).ToArray());
            users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword($"{username}123"),
                Role = UserRole.Manager,
                DepartmentId = dept.Id,
                IsActive = true
            });
        }

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}