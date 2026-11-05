using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A data set as the subject of a threshold; its link table is data.DataSetThreshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "DataSet")]
public sealed class DataSetThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetThresholdSubjectKind"/> class.</summary>
    public DataSetThresholdSubjectKind()
        : base("DataSet", "A data set", "data", "DataSetThreshold")
    {
    }
}
