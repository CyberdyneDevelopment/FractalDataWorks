using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A run in flight writes a table this run writes.</summary>
[TypeOption(typeof(RunWaitReasons), "SharedTable")]
public sealed class SharedTableRunWaitReason : RunWaitReasonBase
{
    /// <summary>Initializes a new instance of the <see cref="SharedTableRunWaitReason"/> class.</summary>
    public SharedTableRunWaitReason() : base("SharedTable", "A run in flight writes a table this run writes.")
    {
    }
}
