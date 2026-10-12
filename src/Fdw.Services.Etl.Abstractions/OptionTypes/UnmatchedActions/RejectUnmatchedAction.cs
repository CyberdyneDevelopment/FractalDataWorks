using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>An unmatched record leaves the stream and is reported with the name of the Branch stage.</summary>
[TypeOption(typeof(UnmatchedActions), "Reject")]
public sealed class RejectUnmatchedAction : UnmatchedActionBase
{
    /// <summary>Initializes a new instance of the <see cref="RejectUnmatchedAction"/> class.</summary>
    public RejectUnmatchedAction() : base("Reject", "Reject the record", "Records that match no path are dropped and reported with the name of the Branch stage; they count toward the pipeline's maximum error count", rejectsRecord: true)
    {
    }
}
