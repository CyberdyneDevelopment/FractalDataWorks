using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The run's tenant has as many runs in flight as its limit allows.</summary>
[TypeOption(typeof(RunWaitReasons), "TenantLimit")]
public sealed class TenantLimitRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantLimitRunWaitReason"/> class.</summary>
    public TenantLimitRunWaitReason() : base("TenantLimit", "The run's tenant has as many runs in flight as its limit allows.")
    {
    }
}
