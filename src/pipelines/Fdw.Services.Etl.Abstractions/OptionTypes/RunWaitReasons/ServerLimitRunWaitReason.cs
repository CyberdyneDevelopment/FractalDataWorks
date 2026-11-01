using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The server is running as many pipeline runs as its limit allows.</summary>
[TypeOption(typeof(RunWaitReasons), "ServerLimit")]
public sealed class ServerLimitRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="ServerLimitRunWaitReason"/> class.</summary>
    public ServerLimitRunWaitReason() : base("ServerLimit", "The server is running as many pipeline runs as its limit allows.")
    {
    }
}
