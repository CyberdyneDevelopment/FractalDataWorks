using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A store answered a keyset page that repeats a key of the page before it, so it did not apply the filter keyset paging needs (FDW-4911).
/// </summary>
/// <remarks>
/// Reading on would return the same page for ever, or repeat rows; the read stops and names the target. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "KeysetFilterIgnored", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeysetFilterIgnoredCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="KeysetFilterIgnoredCode"/> class.</summary>
    public KeysetFilterIgnoredCode()
        : base(20011, "KeysetFilterIgnored",
            ResultSeverities.ByName("Error"),
            "'{target}' answered a page that repeats key ({key}) from the page before it: the store does not apply the filter keyset paging needs, so it cannot be read in pages by key",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
