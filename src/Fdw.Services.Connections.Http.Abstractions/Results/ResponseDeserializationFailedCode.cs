using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to deserialize HTTP response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "ResponseDeserializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ResponseDeserializationFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseDeserializationFailedCode"/> class.
    /// </summary>
    public ResponseDeserializationFailedCode()
        : base(90003, "ResponseDeserializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to deserialize response: {ErrorMessage}",
            isRetryable: false)
    {
    }
}