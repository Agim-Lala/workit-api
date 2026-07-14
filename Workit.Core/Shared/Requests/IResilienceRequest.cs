namespace Workit.Core.Shared.Requests;

public interface IResilienceRequest<out T>
{
    public T FallBack { get; }
}
