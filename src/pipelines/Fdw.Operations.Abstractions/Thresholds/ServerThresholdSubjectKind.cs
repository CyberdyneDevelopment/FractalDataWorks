using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// The server as the subject of a threshold (FDW-3936): what a host judges about itself, such as the runs waiting on it. It has no domain row, so the SubjectId of its links is
/// the Id of the tenant the threshold belongs to, and its link table workflow.ServerThreshold has no foreign key on SubjectId.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdSubjectKinds), "Server")]
public sealed class ServerThresholdSubjectKind : ThresholdSubjectKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ServerThresholdSubjectKind"/> class.</summary>
    public ServerThresholdSubjectKind()
        : base("Server", "The server a host runs on", "workflow", "ServerThreshold")
    {
    }
}
