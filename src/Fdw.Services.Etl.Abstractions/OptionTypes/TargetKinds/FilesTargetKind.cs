using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The source reads a list of files, one after another, as one stream: the declared container read at each name of a list the run supplies.
/// Every file has the fields of the first. A destination of this kind writes one file for each value of the field it splits by (FDW-3193).
/// </summary>
[TypeOption(typeof(TargetKinds), "Files")]
public sealed class FilesTargetKind : TargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FilesTargetKind"/> class.</summary>
    public FilesTargetKind() : base(
        "Files",
        "The source reads a list of files, one after another, as one stream: the declared container read at each name of a list the run supplies.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Needs => ["filesList"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Refuses => ["physicalName", "splitField"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesNeed => ["splitField"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesRefuse => ["physicalName", "filesList"];
}
