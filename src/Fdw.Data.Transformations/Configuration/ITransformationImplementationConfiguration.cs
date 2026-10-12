using Fdw.Configuration;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>The contract every Transformation implementation carries.</summary>
/// <remarks>
/// A transformation is defined once and used later (FDW-846, FDW-962). The domain row
/// (<c>transform.Transformation</c>) carries identity only; the implementation says what it does. A
/// Field implementation transforms one column and is attached to field mappings; the set
/// implementations (Map, Filter, Calculate, Aggregate, Lookup) transform whole inputs and run in
/// pipeline stages.
/// </remarks>
public interface ITransformationImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this transformation is a seeded standard one, owned by the global tenant,
    /// rather than a tenant's own.
    /// </summary>
    bool IsSystemProvided { get; set; }
}
