namespace Fdw.Services.Data.Endpoints;

/// <summary>Request naming a category: the route of the endpoints that act on one.</summary>
public class CategoryNameRequest
{
    /// <summary>Gets or sets the category's stable name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}
