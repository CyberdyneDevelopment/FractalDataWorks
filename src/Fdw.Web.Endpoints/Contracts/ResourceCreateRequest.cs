using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base request for creating a new named resource.
/// Provides the Name property with standard validation.
/// Derived classes add domain-specific required fields.
/// </summary>
public abstract class ResourceCreateRequest
{
    /// <summary>
    /// Gets or sets the unique name for the new resource.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
