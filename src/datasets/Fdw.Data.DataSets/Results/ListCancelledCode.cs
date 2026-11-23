using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The caller's token was cancelled while the list of data sets was being found (FDW-6119): the search stops and answers with this, not with a
/// half-read list and not with an exception. Not a fault of the request; nothing was changed.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "ListCancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ListCancelledCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListCancelledCode"/> class.
    /// </summary>
    public ListCancelledCode()
        : base(10010, "ListCancelled",
            ResultSeverities.ByName("Error"),
            "The list of data sets was cancelled before it was read",
            isRetryable: true)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
