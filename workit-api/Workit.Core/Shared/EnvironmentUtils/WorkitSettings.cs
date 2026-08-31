namespace Workit.Core.Shared.EnvironmentUtils;

public sealed record WorkitSettings(
    DatabaseSettings Database,
    RedisCacheSettings RedisCache,
    HangfireSettings Hangfire,
    TokenSettings Token,
    SentrySettings Sentry,
    CorsSettings Cors,
    AuthSettings Auth)
{
    public static WorkitSettings FromEnvironment()
    {
        return new WorkitSettings(
            new DatabaseSettings(
                Get("CONNECTION_STRING_MAIN", "Host=localhost;Port=5543;Database=workit_dev;Username=postgres;Password=root"),
                GetOptional("CONNECTION_STRING_READ_REPLICA")),
            new RedisCacheSettings(GetOptional("REDIS_CACHE_SERVER_URL")),
            new HangfireSettings(GetBool("HANGFIRE_ENABLED", false)),
            new TokenSettings(
                Get("JWT_SECRET", "replace-me-with-a-long-local-secret"),
                GetInt("JWT_EXPIRATION_IN_MINUTES", 60),
                GetInt("JWT_REFRESH_TOKEN_EXPIRATION_IN_DAYS", 7)),
            new SentrySettings(
                GetOptional("SENTRY_DSN"),
                GetDouble("SENTRY_TRACES_SAMPLE_RATE", 1.0),
                Get("SENTRY_ENVIRONMENT", "development")),
            new CorsSettings(Get("CORS_ALLOWED_ORIGINS", "http://localhost:3000,http://localhost:5173,http://localhost:5187")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
            new AuthSettings(GetBool("AUTH_ENABLE_WHITELIST", false)));
    }

    private static string Get(string name, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? GetOptional(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool GetBool(string name, bool fallback)
    {
        return bool.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    }

    private static int GetInt(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    }

    private static double GetDouble(string name, double fallback)
    {
        return double.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : fallback;
    }
}

public sealed record DatabaseSettings(string MainConnectionString, string? ReadConnectionString = null);
public sealed record RedisCacheSettings(string? Url = null);
public sealed record HangfireSettings(bool Enabled = false);
public sealed record TokenSettings(string Secret, int ExpirationInMinutes = 60, int RefreshTokenExpirationInDays = 7);
public sealed record SentrySettings(string? Dsn = null, double TracesSampleRate = 1.0, string Environment = "development");
public sealed record CorsSettings(string[] AllowedOrigins);
public sealed record AuthSettings(bool EnableWhitelist = false);
