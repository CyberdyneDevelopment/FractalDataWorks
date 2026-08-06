using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base request for updating an existing named resource.
/// Name identifies the resource (typically from route); derived classes
/// add nullable properties for partial update semantics.
/// </summary>
public abstract class ResourceUpdateRequest
{
    /// <summary>
    /// Gets or sets the resource name (typically bound from route).
    /// Identifies which resource to update.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
