using Shouldly;
using Workit.Core.Shared.EnvironmentUtils;

namespace Workit.Core.Tests;

public sealed class SettingsTests
{
    [Fact]
    public void Default_database_name_is_project_specific()
    {
        var settings = WorkitSettings.FromEnvironment();

        settings.Database.MainConnectionString.ShouldContain("Database=workit_dev");
    }
}
