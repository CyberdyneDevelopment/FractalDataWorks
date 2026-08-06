using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Messages;

namespace Fdw.Hosting.Startup;

/// <summary>
/// Records the result of a single bootstrap step.
/// </summary>
// Why: pure data holder — init-only auto-properties populated by StartupResult, no logic of its own.
[ExcludeFromCodeCoverage]
public sealed class StartupStepResult
{
    public string Phase { get; init; } = "";

    public string StepName { get; init; } = "";

    public bool IsSuccess { get; init; }

    public bool IsFatal { get; init; }

    public IGenericMessage? Message { get; init; }

    public Exception? Exception { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
