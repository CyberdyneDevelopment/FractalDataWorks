using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Execute at UTC timestamp is required for Once triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "OnceExecuteAtRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OnceExecuteAtRequiredCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OnceExecuteAtRequiredCode"/> class.
    /// </summary>
    public OnceExecuteAtRequiredCode()
        : base(21000, "OnceExecuteAtRequired",
            ResultSeverities.ByName("Error"),
            "Execute at UTC timestamp is required for Once triggers",
            isRetryable: false)
    {
    }
}