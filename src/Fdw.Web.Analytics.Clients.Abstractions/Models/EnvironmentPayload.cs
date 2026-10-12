using System;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a deployment environment available for promotions.
/// </summary>
public sealed class EnvironmentPayload : IEnvironmentInfo
{
    /// <summary>
    /// Gets or sets the unique identifier of the environment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the environment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the environment.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
