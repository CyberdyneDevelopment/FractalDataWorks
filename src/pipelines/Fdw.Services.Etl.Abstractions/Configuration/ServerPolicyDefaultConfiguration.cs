using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// The pipe.ServerPolicyDefaultConfiguration row: the server's policy defaults, under one ServerPolicyDefault domain row.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ServerPolicyDefaultConfiguration : IServerPolicyDefaultImplementationConfiguration
{
    /// <inheritdoc/>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid Id { get; set; }

    // Why it maps with no column of its own: the name is the domain row's, joined in by the domain provider.
    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string StepFailurePolicy { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string StageFailurePolicy { get; set; } = string.Empty;

    /// <inheritdoc/>
    public int MaxParallelPipelines { get; set; }

    /// <inheritdoc/>
    public bool RequireApprovalToRun { get; set; }

    /// <inheritdoc/>
    public bool AllowResume { get; set; }

    /// <inheritdoc/>
    public bool AllowCrossTenant { get; set; }

    /// <inheritdoc/>
    public Guid? ResiliencyPolicyId { get; set; }
}
