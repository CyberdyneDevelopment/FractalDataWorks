using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.Endpoints.Contracts;

/// <summary>
/// Abstract base request for operations that identify a resource by ID.
/// Used when resources are identified by GUID rather than name.
/// </summary>
public abstract class ResourceIdRequest
{
    /// <summary>
    /// Gets or sets the resource ID (typically bound from route).
    /// </summary>
    [Required]
    public Guid Id { get; set; }
}