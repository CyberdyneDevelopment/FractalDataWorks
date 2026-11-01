using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A run that must run alone is in flight.</summary>
[TypeOption(typeof(RunWaitReasons), "RunningAlone")]
public sealed class RunningAloneRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="RunningAloneRunWaitReason"/> class.</summary>
    public RunningAloneRunWaitReason() : base("RunningAlone", "A run that must run alone is in flight.")
    {
    }
}
