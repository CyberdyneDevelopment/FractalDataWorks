using System;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Environment information.
/// </summary>
public sealed class EnvironmentPayload : IEnvironmentInfo
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the environment name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the environment description.</summary>
    public string Description { get; set; } = string.Empty;
}
