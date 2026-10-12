using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The column is compared to decide whether a matched row changed.</summary>
[TypeOption(typeof(PipelineLoadColumnRoles), "Compare")]
public sealed class ComparePipelineLoadColumnRole : PipelineLoadColumnRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="ComparePipelineLoadColumnRole"/> class.</summary>
    public ComparePipelineLoadColumnRole() : base("Compare", belongsToValueSource: false, isMatchKey: false, isComparison: true, isWritten: false)
    {
    }
}
