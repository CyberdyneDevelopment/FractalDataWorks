using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A read was asked to stream rows in a stable order and none is available: the store's connection cannot open a cursor and the container declares no primary key to page by (FDW-4911).
/// </summary>
/// <remarks>
/// Rows are never paged in no stated order, because a store with no stable order can then repeat or skip a row between pages, and no key is guessed. The message names the container and what it needs. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "StableReadUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StableReadUnavailableCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="StableReadUnavailableCode"/> class.</summary>
    public StableReadUnavailableCode()
        : base(20008, "StableReadUnavailable",
            ResultSeverities.ByName("Error"),
            "'{target}' cannot be read in a stable order: {reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
