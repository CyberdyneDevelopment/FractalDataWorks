using System;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base for full resource detail returned from Get/Create/Update operations.
/// Derived classes add domain-specific detail fields.
/// </summary>
public abstract class ResourceDetail : INamedResource
{
    /// <summary>
    /// Gets or sets the resource unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the resource name.
    /// </summary>
    public required string Name { get; set; }
}
