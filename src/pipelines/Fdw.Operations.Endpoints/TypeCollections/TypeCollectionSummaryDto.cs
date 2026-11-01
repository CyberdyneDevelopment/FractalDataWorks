namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Summary of a TypeCollection loaded in this process.
/// </summary>
public sealed class TypeCollectionSummaryDto
{
    /// <summary>The collection's class name, which is what the values endpoint takes.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Either <c>type</c> (a TypeCollection) or <c>service-type</c> (a ServiceTypeCollection).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The number of options the collection holds in this process.</summary>
    public int ValueCount { get; set; }
}
