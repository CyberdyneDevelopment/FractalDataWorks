using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The column is part of the match key.</summary>
[TypeOption(typeof(PipelineLoadColumnRoles), "Key")]
public sealed class KeyPipelineLoadColumnRole : PipelineLoadColumnRoleBase
{
    /// <summary>Initializes a new instance of the <see cref="KeyPipelineLoadColumnRole"/> class.</summary>
    public KeyPipelineLoadColumnRole() : base("Key", belongsToValueSource: false, isMatchKey: true, isComparison: false, isWritten: false)
    {
    }
}
