namespace Workit.Core.Shared.Persistence.DataSeeders;

public interface IDataSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken = default);
}
