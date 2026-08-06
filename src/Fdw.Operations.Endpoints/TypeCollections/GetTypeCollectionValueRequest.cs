namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Request to get detail for a specific TypeOption within a TypeCollection.
/// </summary>
public sealed class GetTypeCollectionValueRequest
{
    /// <summary>Gets or sets the TypeCollection name (e.g., "ConnectionTypes").</summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the TypeOption name (e.g., "MsSql").</summary>
    public string TypeName { get; set; } = string.Empty;
}
