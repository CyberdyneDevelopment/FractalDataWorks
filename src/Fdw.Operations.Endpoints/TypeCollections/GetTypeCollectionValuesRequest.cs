namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Request to list all values for a named TypeCollection.
/// </summary>
public sealed class GetTypeCollectionValuesRequest
{
    /// <summary>Gets or sets the TypeCollection name (e.g., "ConnectionTypes").</summary>
    public string CollectionName { get; set; } = string.Empty;
}
