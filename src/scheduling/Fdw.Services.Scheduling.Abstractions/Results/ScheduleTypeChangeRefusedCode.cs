using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// An update states a scheduler type other than the one the schedule is stored under (FDW-1171).
/// </summary>
/// <remarks>
/// 2xxxx (Validation): the type is the caller's input, so the answer is a 400 that names both types. A schedule's type is the
/// implementation its row is saved under, so changing it would move the row between implementations; an update never has, and
/// it used to accept the member and ignore it. It is refused instead: the caller creates a new schedule of the other type.
/// </remarks>
[TypeOption(typeof(SchedulingResultCodes), "ScheduleTypeChangeRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ScheduleTypeChangeRefusedCode : SchedulingResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleTypeChangeRefusedCode"/> class.</summary>
    public ScheduleTypeChangeRefusedCode()
        : base(21007, "ScheduleTypeChangeRefused",
            ResultSeverities.ByName("Error"),
            "schedulerType '{StatedType}' cannot replace '{StoredType}': a schedule's type cannot be changed; create a new schedule of that type instead",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller both types: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
