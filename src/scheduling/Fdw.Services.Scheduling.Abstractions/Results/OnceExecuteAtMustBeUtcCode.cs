using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Execute at timestamp must be in UTC for Once triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "OnceExecuteAtMustBeUtc", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OnceExecuteAtMustBeUtcCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnceExecuteAtMustBeUtcCode"/> class.
    /// </summary>
    public OnceExecuteAtMustBeUtcCode()
        : base(21004, "OnceExecuteAtMustBeUtc",
            ResultSeverities.ByName("Error"),
            "Execute at timestamp must be in UTC for Once triggers",
            isRetryable: false)
    {
    }
}