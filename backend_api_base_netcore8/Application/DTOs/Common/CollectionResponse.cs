namespace backend_api_base_netcore8.Application.DTOs.Common;

public class CollectionResponse<T>
{
    public bool Success { get; set; }
    public IReadOnlyCollection<T> Data { get; set; } = Array.Empty<T>();
    public CollectionMeta Meta { get; set; } = new();
}

public class CollectionMeta
{
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
