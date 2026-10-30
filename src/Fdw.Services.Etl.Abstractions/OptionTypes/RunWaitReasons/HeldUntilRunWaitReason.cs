using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run was triggered while its pipeline was at its limit, and its pipeline holds such a run until a set time.</summary>
[TypeOption(typeof(RunWaitReasons), "HeldUntil")]
public sealed class HeldUntilRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="HeldUntilRunWaitReason"/> class.</summary>
    public HeldUntilRunWaitReason() : base("HeldUntil", "The run was triggered while its pipeline was at its limit, and its pipeline holds such a run until a set time, when it joins the queue.")
    {
    }
}
