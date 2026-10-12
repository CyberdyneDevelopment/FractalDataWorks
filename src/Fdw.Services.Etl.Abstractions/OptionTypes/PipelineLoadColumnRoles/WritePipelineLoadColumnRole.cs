using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The column is written by the load.</summary>
[TypeOption(typeof(PipelineLoadColumnRoles), "Write")]
public sealed class WritePipelineLoadColumnRole : PipelineLoadColumnRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="WritePipelineLoadColumnRole"/> class.</summary>
    public WritePipelineLoadColumnRole() : base("Write", belongsToValueSource: false, isMatchKey: false, isComparison: false, isWritten: true)
    {
    }
}
