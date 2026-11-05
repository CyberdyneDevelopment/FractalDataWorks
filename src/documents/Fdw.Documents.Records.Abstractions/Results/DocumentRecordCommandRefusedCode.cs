using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Results;

/// <summary>The 'Command' command was refused before it was sent: Reason. (Validation, 20457.)</summary>
[TypeOption(typeof(DocumentRecordResultCodes), "DocumentRecordCommandRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentRecordCommandRefusedCode : DocumentResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRecordCommandRefusedCode"/> class.</summary>
    public DocumentRecordCommandRefusedCode()
        : base(20457, "DocumentRecordCommandRefused",
            ResultSeverities.ByName("Error"),
            "The '{Command}' command was refused before it was sent: {Reason}.",
            isRetryable: false)
    {
    }
}
