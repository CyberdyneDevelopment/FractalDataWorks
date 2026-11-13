using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A preview of a Graph data set draft stated a cap on the rows read from each source that is not at least one
/// </summary>
/// <remarks>
/// A preview of a Graph data set draft stated a cap on the rows read from each source that is not at least one (FDW-5454). Nothing is read; no cap is assumed in its place. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetPreviewCapInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetPreviewCapInvalidCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetPreviewCapInvalidCode"/> class.</summary>
    public GraphDataSetPreviewCapInvalidCode()
        : base(21412, "GraphDataSetPreviewCapInvalid",
            ResultSeverities.ByName("Error"),
            "The preview of Graph data set draft '{dataSet}' states {cap} rows from each source, which is not a whole number of at least one, so nothing is read",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which cap their preview request stated and why it is refused.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
