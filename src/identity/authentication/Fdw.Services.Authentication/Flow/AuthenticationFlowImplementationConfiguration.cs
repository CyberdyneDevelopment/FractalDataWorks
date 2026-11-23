using System;
using System.Collections.Generic;
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
    // Why [NotStored]: a flow's description is a row of the descriptions table under the description type AuthenticationFlow, written in the unit of the flow's save (FDW-6208);
    // the column is gone from auth.AuthenticationFlowConfiguration. It is never filled on load, so it is null until a caller states one, and it has no initializer.
    [NotStored]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string Audience { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? MinimumAcr { get; set; }

    /// <inheritdoc/>
    public string ExecutionLifetime { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? SecretManagerName { get; set; }

    /// <inheritdoc/>
    public string? SecretKeyName { get; set; }

    /// <inheritdoc/>
    public string? ReturnUrls { get; set; }

    /// <inheritdoc/>
    public string? CodeLifetime { get; set; }

    /// <inheritdoc/>
    public string? SessionServiceName { get; set; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public IList<AuthenticationFlowStepConfiguration> Steps { get; set; } = [];

}
