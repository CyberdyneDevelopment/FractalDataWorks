using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A note or an intended source was to be saved and the caller cannot be named.
/// </summary>
/// <remarks>Every new note and intended source records who made it and when (FDW-5452); a caller that cannot be named is refused, never stored as no author. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetAuthorUnresolved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetAuthorUnresolvedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetAuthorUnresolvedCode"/> class.</summary>
    public GraphDataSetAuthorUnresolvedCode()
        : base(21430, "GraphDataSetAuthorUnresolved",
            ResultSeverities.ByName("Error"),
            "Graph data set '{dataSet}' cannot take a note or an intended source: the caller cannot be named, so there is no author to record",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which data set cannot take the note or intended source, because nobody can be named as its author.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
