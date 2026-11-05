using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A connection as the subject of a threshold; its link table is conn.ConnectionThreshold.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "Connection")]
public sealed class ConnectionThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionThresholdSubjectKind"/> class.</summary>
    public ConnectionThresholdSubjectKind()
        : base("Connection", "A connection", "conn", "ConnectionThreshold")
    {
    }
}
