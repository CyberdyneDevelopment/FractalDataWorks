using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's FilesList.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "FilesList")]
public sealed class FilesListTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="FilesListTargetSetting"/> class.</summary>
    public FilesListTargetSetting()
        : base("FilesList", "filesList")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.FilesList;
}
