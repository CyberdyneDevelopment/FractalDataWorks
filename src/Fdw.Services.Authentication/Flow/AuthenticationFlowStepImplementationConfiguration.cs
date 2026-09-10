using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication.Flow;

/// <summary>AuthenticationFlowStep as configured.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AuthenticationFlowStepImplementationConfiguration : IAuthenticationFlowStepImplementationConfiguration
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
    public Guid AuthenticationFlowId { get; set; }

    /// <inheritdoc/>
    public int AuthenticationFlowRowId { get; set; }

    /// <inheritdoc/>
    public int StepOrder { get; set; }

    /// <inheritdoc/>
    public string StepName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Configuration { get; set; }

}
