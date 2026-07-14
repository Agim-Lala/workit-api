using Microsoft.EntityFrameworkCore;

namespace Workit.Core.Shared.Persistence.DataMigrators;

public sealed class EfDataMigrator(AppDbContext appDbContext) : IDataMigrator
{
    public void Migrate()
    {
        if (appDbContext.Database.GetPendingMigrations().Any())
        {
            appDbContext.Database.Migrate();
        }
    }
}
