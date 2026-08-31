using Microsoft.AspNetCore.Authorization;
using Workit.Core.Users.Domain;

namespace Workit.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string BusinessOnly = nameof(BusinessOnly);
    public const string WorkerOnly = nameof(WorkerOnly);

    public static void AddWorkitPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy => policy.RequireRole(UserRole.Admin.ToString()));
        options.AddPolicy(BusinessOnly, policy => policy.RequireRole(UserRole.Business.ToString()));
        options.AddPolicy(WorkerOnly, policy => policy.RequireRole(UserRole.Worker.ToString()));
    }
}
