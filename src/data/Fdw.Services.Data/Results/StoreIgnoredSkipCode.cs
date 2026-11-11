using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A store read in pages in its own order answered a full page equal, row for row, to the page before it (FDW-4911).
/// </summary>
/// <remarks>
/// Either the store ignores Skip, so reading on would return the same page for ever, or the data holds two full pages of identical rows, which
/// pages by position cannot tell apart. The read stops and names the target; a keyset or stream read reads that data. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "StoreIgnoredSkip", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class StoreIgnoredSkipCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="StoreIgnoredSkipCode"/> class.</summary>
    public StoreIgnoredSkipCode()
        : base(20015, "StoreIgnoredSkip",
            ResultSeverities.ByName("Error"),
            "'{target}' answered the same page twice (the page after row {skip}): either the store ignores Skip or the data holds two full pages of identical rows, so it cannot be read in pages by position; read it by key (declare a key) or as a stream",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
