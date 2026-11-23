using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// The caller's token was cancelled while a record's citation was being checked (FDW-6221): the check stops and answers with this, not with an
/// exception and not with a guess about the cited member. The citing record was not written.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "CitationCheckCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CitationCheckCancelledCode : ServicesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="CitationCheckCancelledCode"/> class.</summary>
    public CitationCheckCancelledCode()
        : base(10010, "CitationCheckCancelled",
            ResultSeverities.ByName("Error"),
            "The check that a cited member exists in '{container}' was cancelled before it was answered",
            isRetryable: true)
    {
    }

    /// <summary>Shows the caller why the write was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
