using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>A note was rejected before it was written.</summary>
/// <remarks>
/// Rejection rather than repair: an over-long body is refused with its length and the limit named,
/// never truncated. A truncation is unrecoverable — the writer sees the note posted and does not
/// see that the end of it is gone.
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseNoteInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseNoteInvalidCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseNoteInvalidCode"/> class.</summary>
    public UniverseNoteInvalidCode()
        : base(20002, "UniverseNoteInvalid",
            ResultSeverities.ByName("Error"),
            "Note in universe '{name}' rejected: {reason}",
            isRetryable: false)
    {
    }
}
