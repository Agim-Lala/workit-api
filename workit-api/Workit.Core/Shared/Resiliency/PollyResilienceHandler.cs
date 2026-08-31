using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;

namespace Workit.Core.Shared.Resiliency;

public sealed class PollyResilienceHandler(ILogger<PollyResilienceHandler> logger) : IResilienceHandler
{
    public async ValueTask<TResult> HandleWithRetryAndFallbackAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        TResult fallback,
        CancellationToken cancellationToken = default)
    {
        var retryPipeline = new ResiliencePipelineBuilder<TResult>()
            .AddRetry(DefaultRetryStrategyOptions<TResult>());
        var fallbackPipeline = new ResiliencePipelineBuilder<TResult>()
            .AddFallback(DefaultFallbackStrategyOptions(fallback));

        return await fallbackPipeline.AddPipeline(retryPipeline.Build())
            .Build()
            .ExecuteAsync(callback, cancellationToken);
    }

    public async ValueTask<TResult> HandleWithCircuitBreakerAndFallbackAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        TResult fallback,
        CancellationToken cancellationToken = default)
    {
        var circuitBreakerPipeline = new ResiliencePipelineBuilder<TResult>()
            .AddCircuitBreaker(DefaultCircuitBreakerStrategyOptions<TResult>());
        var fallbackPipeline = new ResiliencePipelineBuilder<TResult>()
            .AddFallback(DefaultFallbackStrategyOptions(fallback));

        return await fallbackPipeline.AddPipeline(circuitBreakerPipeline.Build())
            .Build()
            .ExecuteAsync(callback, cancellationToken);
    }

    public async ValueTask<TResult> HandleWithRetryAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> callback,
        CancellationToken cancellationToken = default)
    {
        var retryPipeline = new ResiliencePipelineBuilder<TResult>()
            .AddRetry(DefaultRetryStrategyOptions<TResult>());

        return await retryPipeline.Build()
            .ExecuteAsync(callback, cancellationToken);
    }

    private CircuitBreakerStrategyOptions<TResult> DefaultCircuitBreakerStrategyOptions<TResult>()
    {
        return new CircuitBreakerStrategyOptions<TResult>
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(5),
            ShouldHandle = new PredicateBuilder<TResult>().Handle<HttpRequestException>(),
            OnOpened = args =>
            {
                logger.LogWarning(
                    "Circuit breaker opened due to {ExceptionType}",
                    args.Outcome.Exception?.GetType().Name);

                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                logger.LogWarning("Circuit breaker closed");

                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                logger.LogWarning("Circuit breaker half-opened");

                return ValueTask.CompletedTask;
            }
        };
    }

    private FallbackStrategyOptions<TResult> DefaultFallbackStrategyOptions<TResult>(TResult fallback)
    {
        return new FallbackStrategyOptions<TResult>
        {
            ShouldHandle = new PredicateBuilder<TResult>().Handle<HttpRequestException>(),
            FallbackAction = _ => Outcome.FromResultAsValueTask(fallback),
            OnFallback = args =>
            {
                logger.LogWarning(
                    "Fallback returned due to {ExceptionMessage}",
                    args.Outcome.Exception?.Message);

                return ValueTask.CompletedTask;
            }
        };
    }

    private RetryStrategyOptions<TResult> DefaultRetryStrategyOptions<TResult>()
    {
        return new RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(2),
            ShouldHandle = new PredicateBuilder<TResult>().Handle<HttpRequestException>(),
            OnRetry = args =>
            {
                logger.LogWarning(
                    "Retry {RetryCount} due to {ExceptionMessage}",
                    args.AttemptNumber,
                    args.Outcome.Exception?.Message);

                return ValueTask.CompletedTask;
            }
        };
    }
}
