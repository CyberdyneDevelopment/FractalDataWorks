using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The source reads, or the destination writes, one file: the declared container at the name the component states (and the path, when it states one).</summary>
[TypeOption(typeof(TargetKinds), "File")]
public sealed class FileTargetKind : TargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FileTargetKind"/> class.</summary>
    public FileTargetKind() : base("File", "The source reads, or the destination writes, one file: the declared container at the name the component states.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Needs => ["physicalName"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Refuses => ["filesList", "splitField"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesNeed => ["physicalName"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesRefuse => ["filesList", "splitField"];
}
