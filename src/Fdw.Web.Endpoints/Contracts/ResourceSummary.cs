namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base for lightweight resource summaries returned in list operations.
/// Derived classes add domain-specific summary fields.
/// </summary>
public abstract class ResourceSummary : INamedResource
{
    /// <summary>
    /// Gets or sets the resource name.
    /// </summary>
    public required string Name { get; set; }
}
