using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base request for operations that identify a resource by name.
/// Used for Get, Delete, and other single-resource operations.
/// </summary>
public abstract class ResourceNameRequest
{
    /// <summary>
    /// Gets or sets the resource name (typically bound from route).
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}