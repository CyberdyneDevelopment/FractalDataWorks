using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>This run must run alone, and other runs are in flight or ahead of it.</summary>
[TypeOption(typeof(RunWaitReasons), "NeedsHostAlone")]
public sealed class NeedsHostAloneRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="NeedsHostAloneRunWaitReason"/> class.</summary>
    public NeedsHostAloneRunWaitReason() : base("NeedsHostAlone", "This run must run alone, and other runs are in flight or ahead of it.")
    {
    }
}
