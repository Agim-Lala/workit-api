namespace Workit.Core.Shared.Resiliency;

public interface IResilienceHandler
{
    public ValueTask<TResult> HandleWithRetryAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        CancellationToken cancellationToken = default);

    public ValueTask<TResult> HandleWithRetryAndFallbackAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        TResult fallback,
        CancellationToken cancellationToken = default);

    public ValueTask<TResult> HandleWithCircuitBreakerAndFallbackAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        TResult fallback,
        CancellationToken cancellationToken = default);
}
