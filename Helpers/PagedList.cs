using Microsoft.EntityFrameworkCore;

namespace ActivityApp.Helpers;

/// <summary>معلومات الترقيم التي يحتاجها الجزء المشترك _Pagination</summary>
public interface IPagedList
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
    int FirstItemIndex { get; }
    int LastItemIndex { get; }
}

/// <summary>
/// صفحة واحدة من نتائج استعلام. ترث List&lt;T&gt; حتى تبقى الواجهات (foreach, Count, Any) تعمل كما هي.
/// </summary>
public class PagedList<T> : List<T>, IPagedList
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public static readonly int[] PageSizeOptions = { 10, 20, 50, 100 };

    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItemIndex => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalCount);

    private PagedList(IEnumerable<T> items, int page, int pageSize, int totalCount) : base(items)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public static int NormalizePageSize(int? pageSize) =>
        pageSize is null or <= 0 ? DefaultPageSize : Math.Min(pageSize.Value, MaxPageSize);

    /// <summary>ينفّذ الاستعلام على قاعدة البيانات مع Skip/Take. يجب أن يكون الاستعلام مرتّباً.</summary>
    public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int? page, int? pageSize)
    {
        var size = NormalizePageSize(pageSize);
        var total = await source.CountAsync();
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)size);
        var current = Math.Clamp(page ?? 1, 1, totalPages);
        var items = await source.Skip((current - 1) * size).Take(size).ToListAsync();
        return new PagedList<T>(items, current, size, total);
    }
}
