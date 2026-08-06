using System;
using Fdw.Services.Etl.Projects.Abstractions;
using Microsoft.Extensions.Options;


namespace Fdw.Services.Etl.Projects.Policy;

/// <summary>
/// Implementation of <see cref="IServerPolicyDefaults"/> that reads from the
/// <c>ProjectServerDefaults</c> appsettings section via IOptions.
/// </summary>
/// <remarks>
/// When a policy field is null in appsettings, the following hard-coded fallback defaults apply:
/// <list type="table">
/// <item><term>StepFailurePolicy</term><description>"HaltStage" (strict fail-fast)</description></item>
/// <item><term>StageFailurePolicy</term><description>"HaltProject" (strict fail-fast)</description></item>
/// <item><term>MaxParallelPipelines</term><description>4</description></item>
/// <item><term>RequireApprovalToRun</term><description>false</description></item>
/// <item><term>AllowResume</term><description>false</description></item>
/// <item><term>AllowCrossTenant</term><description>false</description></item>
/// <item><term>ResiliencyPolicyId</term><description>null (no resiliency)</description></item>
/// </list>
/// </remarks>
public sealed class ServerPolicyDefaults : IServerPolicyDefaults
{
    private readonly IOptions<ServerPolicyDefaultsOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerPolicyDefaults"/> class.
    /// </summary>
    public ServerPolicyDefaults(IOptions<ServerPolicyDefaultsOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    // Why: Hard-coded "HaltStage" is the strict/safe default; appsettings can loosen to "ContinueStage".
    public string StepFailurePolicy => _options.Value.StepFailurePolicy ?? "HaltStage";

    /// <inheritdoc/>
    // Why: Hard-coded "HaltProject" is the strict/safe default; appsettings can loosen to "ContinueProject".
    public string StageFailurePolicy => _options.Value.StageFailurePolicy ?? "HaltProject";

    /// <inheritdoc/>
    // Why: 4 is a reasonable parallelism cap; most servers handle 4 concurrent pipelines without pressure.
    public int MaxParallelPipelines => _options.Value.MaxParallelPipelines ?? 4;

    /// <inheritdoc/>
    public bool RequireApprovalToRun => _options.Value.RequireApprovalToRun ?? false;

    /// <inheritdoc/>
    public bool AllowResume => _options.Value.AllowResume ?? false;

    /// <inheritdoc/>
    public bool AllowCrossTenant => _options.Value.AllowCrossTenant ?? false;

    /// <inheritdoc/>
    public Guid? ResiliencyPolicyId => _options.Value.ResiliencyPolicyId;
}
