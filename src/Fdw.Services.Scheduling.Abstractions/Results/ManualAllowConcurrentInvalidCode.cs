using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// Allow concurrent must be a boolean value for Manual triggers.
/// </summary>
[TypeOption(typeof(SchedulingResultCodes), "ManualAllowConcurrentInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ManualAllowConcurrentInvalidCode : SchedulingResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManualAllowConcurrentInvalidCode"/> class.
    /// </summary>
    public ManualAllowConcurrentInvalidCode()
        : base(21003, "ManualAllowConcurrentInvalid",
            ResultSeverities.ByName("Error"),
            "Allow concurrent must be a boolean value for Manual triggers",
            isRetryable: false)
    {
    }
}