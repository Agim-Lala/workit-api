namespace Workit.Core.Shared.Services;

public interface IService<TResponse>
{
    public Task<TResponse> HandleAsync(CancellationToken cancellationToken = default);
}

public interface IService<in TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
