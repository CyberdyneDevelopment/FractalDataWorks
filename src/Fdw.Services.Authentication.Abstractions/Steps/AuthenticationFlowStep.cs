using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// One step of a flow: the registered step it selects, and that step's settings for this flow.
/// </summary>
/// <remarks>
/// The settings belong to the flow's row, not to the step. One step runs in many flows, and each flow
/// configures it separately — which is why a step that reads settings is handed them per run rather
/// than holding them.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthenticationFlowStep
{
    /// <summary>Gets the registered name of the step to run.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the step's own settings for this flow, as JSON; null when the row carries none.</summary>
    public required string? Configuration { get; init; }
}
