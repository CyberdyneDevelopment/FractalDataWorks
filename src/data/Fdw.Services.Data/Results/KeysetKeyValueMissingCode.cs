using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A row read in keyset pages carries no value for a field of the key the pages follow (FDW-4911).
/// </summary>
/// <remarks>
/// The next page asks for the rows after the last key, and a missing or null key value cannot be compared, so the read stops rather than skip or repeat rows. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "KeysetKeyValueMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class KeysetKeyValueMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="KeysetKeyValueMissingCode"/> class.</summary>
    public KeysetKeyValueMissingCode()
        : base(20010, "KeysetKeyValueMissing",
            ResultSeverities.ByName("Error"),
            "'{target}' returned a row with no value for key field '{field}', so the rows after it cannot be asked for by key",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what was refused and why: the message names their own target.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
