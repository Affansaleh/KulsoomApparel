using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.AppDbContext;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseSqlServer(
    "Data Source=.\\SQLEXPRESS;Initial Catalog=KulsoomApparelDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}