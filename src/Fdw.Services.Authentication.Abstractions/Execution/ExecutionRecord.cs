using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Context;

namespace Fdw.Services.Authentication.Abstractions.Execution;

/// <summary>
/// A flow suspended part-way, waiting for its caller to come back.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ExecutionRecord
{
    /// <summary>Gets the execution identifier, safe to log.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the flow being run.</summary>
    public required string FlowName { get; init; }

    /// <summary>Gets what the flow had established when it suspended.</summary>
    public required AuthenticationContext Context { get; init; }

    /// <summary>Gets the index of the step that suspended, so the flow resumes at it.</summary>
    public required int CurrentStepIndex { get; init; }

    /// <summary>Gets when this execution stops being resumable.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
