using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A schedule's description: what the schedule is for, in the words of whoever configured it (FDW-6208). Read by whoever holds <c>schedules:read</c>. The schedule is the thing described, so the type has no owner: a schedule is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class ScheduleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the schedule provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Schedule";

    /// <summary>Initializes a new instance of the <see cref="ScheduleDescriptionType"/> class.</summary>
    public ScheduleDescriptionType()
        : base(OptionName, "schedules:read", hasOwner: false)
    {
    }
}
