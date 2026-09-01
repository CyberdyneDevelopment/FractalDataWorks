using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A named, ordered composition of steps.
/// </summary>
/// <remarks>
/// The order is data, not code — which is what stops every login being bespoke. It is still an
/// order rather than a graph: no branches, no loops. A step that needs to decide something decides
/// it inside itself, in code that can be tested, rather than in configuration that cannot.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthenticationFlow
{
    /// <summary>Gets the flow name — what a caller selects.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the steps, in the order they run.</summary>
    public required IReadOnlyList<string> Steps { get; init; }

    /// <summary>Gets the audience tokens from this flow are minted for.</summary>
    public required string Audience { get; init; }

    /// <summary>Gets the assurance level this flow demands before a token may be issued.</summary>
    public string? MinimumAcr { get; init; }

    /// <summary>Gets how long a flow suspended mid-way (e.g. awaiting an OIDC redirect) stays resumable.</summary>
    public required TimeSpan ExecutionLifetime { get; init; }
}
