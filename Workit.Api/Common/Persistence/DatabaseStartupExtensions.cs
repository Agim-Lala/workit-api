using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataMigrators;
using Workit.Core.Shared.Persistence.DataSeeders;

namespace Workit.Api.Common.Persistence;

public static class DatabaseStartupExtensions
{
    public static async Task<WebApplication> ApplyMigrationsAndSeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.ProviderName is not "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return app;
        }

        var migrator = scope.ServiceProvider.GetRequiredService<IDataMigrator>();
        migrator.Migrate();

        var dataSeeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await dataSeeder.SeedAsync();

        return app;
    }
}
