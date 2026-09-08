using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>A resource attached to a dataverse.</summary>
/// <remarks>
/// <c>ResourceType</c> is deliberately an open string, not a closed set. The kinds a host
/// understands are the domains it has referenced, so a client should treat this as a string with a
/// known-values list it refreshes, never as an exhaustive union.
/// </remarks>
public class DataverseResourceDto
{
    /// <summary>Gets or sets the attachment's logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the kind of resource attached.</summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached resource's logical identity.</summary>
    public Guid ResourceId { get; set; }

    /// <summary>Gets or sets how the dataverse relates to it: Owns, Uses or Produces.</summary>
    public string Relationship { get; set; } = string.Empty;
}
