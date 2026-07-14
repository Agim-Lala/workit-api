namespace Workit.Core.Shared.Requests;

public abstract record PagedRequest(int Page = 1, int PageSize = 25)
{
    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
}
