using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for operations that require a container identifier.
/// </summary>
public class ContainerIdRequest
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }
}
