using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A failing record leaves the stream and is reported with the name of the check it failed.</summary>
[TypeOption(typeof(ValidateFailActions), "Reject")]
public sealed class RejectValidateFailAction : ValidateFailActionBase
{
    /// <summary>Initializes a new instance of the <see cref="RejectValidateFailAction"/> class.</summary>
    public RejectValidateFailAction() : base("Reject", "Reject the record", "The record leaves the stream and is reported with the name of the check it failed; it counts toward the pipeline's maximum error count", stopsRun: false)
    {
    }
}
