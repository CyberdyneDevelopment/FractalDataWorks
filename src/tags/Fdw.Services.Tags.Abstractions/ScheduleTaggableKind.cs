using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A Schedule that carries tags in <c>sched.ScheduleTag</c>. This kind is tenant-wide: it is in no workspace, so it takes tenant tags only.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "Schedule")]
public sealed class ScheduleTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleTaggableKind"/> class.</summary>
    public ScheduleTaggableKind()
        : base("Schedule", "sched", "ScheduleTag", "ScheduleId", string.Empty)
    {
    }
}
