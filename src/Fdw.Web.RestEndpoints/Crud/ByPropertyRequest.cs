namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Uniform request DTO for endpoints that look up a resource by a single key property
/// (Name, Id, Code, Slug, etc.). The route always binds the key via <c>{Key}</c>.
/// </summary>
/// <typeparam name="TKey">Key type — typically <see cref="string"/> for name-based or
/// <see cref="System.Guid"/> for id-based lookups.</typeparam>
public sealed class ByPropertyRequest<TKey>
    where TKey : notnull
{
    /// <summary>Gets or sets the key value extracted from the route.</summary>
    public TKey Key { get; set; } = default!;
}
