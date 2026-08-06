using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to extract rows from a REST response body.
/// Raised when the row-extraction path (IEnumerable&lt;Dictionary&gt; resultType) encounters
/// a JSON parse error or when the record selector resolves to a non-array element.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "ResponseRowExtractionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ResponseRowExtractionFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseRowExtractionFailedCode"/> class.
    /// </summary>
    public ResponseRowExtractionFailedCode()
        : base(91003, "ResponseRowExtractionFailed",
            ResultSeverities.ByName("Error"),
            "Failed to extract rows from response: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
