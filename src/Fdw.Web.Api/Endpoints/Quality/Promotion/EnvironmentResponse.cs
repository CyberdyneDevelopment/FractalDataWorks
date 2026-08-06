using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing a deployment environment.
/// </summary>
public class EnvironmentResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the environment name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the promotion order (lower values promote first).</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the connection name for this environment.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether approval is required for promotions to this environment.</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>Gets or sets the approvers for this environment.</summary>
    public IReadOnlyList<string> Approvers { get; set; } = [];

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
}
