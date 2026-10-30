using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The destination writes the container of the data set it names, at the address its source declares (FDW-3193).</summary>
[TypeOption(typeof(TargetKinds), "DataSet")]
public sealed class DataSetTargetKind : TargetKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetTargetKind"/> class.</summary>
    public DataSetTargetKind() : base("DataSet", "The destination writes the container of the data set it names, at the address its source declares.")
    {
    }

    /// <inheritdoc/>
    public override bool ForSources => false;

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesNeed => ["dataSet"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> WritesRefuse => ["physicalPath", "physicalName", "filesList", "splitField"];
}
