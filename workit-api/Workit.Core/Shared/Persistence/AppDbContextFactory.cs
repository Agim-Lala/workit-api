using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Workit.Core.Shared.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING_MAIN")
            ?? "Host=localhost;Port=5543;Database=workit_dev;Username=postgres;Password=root";

        builder.UseNpgsql(connectionString, options =>
        {
            options.EnableRetryOnFailure(3);
        });

        return new AppDbContext(builder.Options);
    }
}
