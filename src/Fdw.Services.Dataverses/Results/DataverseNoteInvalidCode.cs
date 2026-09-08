using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>A note was rejected before it was written.</summary>
/// <remarks>
/// Rejection rather than repair: an over-long body is refused with its length and the limit named,
/// never truncated. A truncation is unrecoverable — the writer sees the note posted and does not
/// see that the end of it is gone.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseNoteInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseNoteInvalidCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseNoteInvalidCode"/> class.</summary>
    public DataverseNoteInvalidCode()
        : base(20002, "DataverseNoteInvalid",
            ResultSeverities.ByName("Error"),
            "Note in dataverse '{name}' rejected: {reason}",
            isRetryable: false)
    {
    }
}
