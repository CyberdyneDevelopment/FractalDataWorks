using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The column orders the rows a sequence value is handed out in.</summary>
[TypeOption(typeof(PipelineLoadColumnRoles), "SequenceOrder")]
public sealed class SequenceOrderPipelineLoadColumnRole : PipelineLoadColumnRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="SequenceOrderPipelineLoadColumnRole"/> class.</summary>
    public SequenceOrderPipelineLoadColumnRole() : base("SequenceOrder", belongsToValueSource: true, isMatchKey: false, isComparison: false, isWritten: false)
    {
    }
}
