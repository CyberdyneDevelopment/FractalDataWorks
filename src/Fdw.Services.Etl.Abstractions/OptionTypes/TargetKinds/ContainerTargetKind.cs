using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The source reads, or the destination writes, the container it declares, at the address it declares.</summary>
[TypeOption(typeof(TargetKinds), "Container")]
public sealed class ContainerTargetKind : TargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerTargetKind"/> class.</summary>
    public ContainerTargetKind() : base("Container", "The source reads, or the destination writes, the container it declares, at the address it declares.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Refuses => ["physicalPath", "physicalName", "filesList", "splitField"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesNeed => ["container"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesRefuse => ["physicalPath", "physicalName", "filesList", "splitField"];
}
