using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.Flow;

/// <summary>AuthenticationFlow as configured.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AuthenticationFlowImplementationConfiguration : IAuthenticationFlowImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid AuthenticationFlowId { get; set; }


    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string Audience { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? MinimumAcr { get; set; }

    /// <inheritdoc/>
    public string ExecutionLifetime { get; set; } = string.Empty;

}
