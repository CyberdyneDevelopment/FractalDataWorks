namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Uniform request DTO for endpoints that update a resource located by a single key
/// property. The key is bound from the route (<c>{Key}</c>) and the update payload
/// is bound from the request body.
/// </summary>
/// <typeparam name="TKey">Key type used to locate the resource.</typeparam>
/// <typeparam name="TBody">The update-payload type containing only the mutable fields.</typeparam>
public sealed class UpdateByPropertyRequest<TKey, TBody>
    where TKey : notnull
    where TBody : class
{
    /// <summary>Gets or sets the key value (bound from the URL).</summary>
    public TKey Key { get; set; } = default!;

    /// <summary>Gets or sets the update payload (bound from the request body).</summary>
    public TBody Body { get; set; } = default!;
}
