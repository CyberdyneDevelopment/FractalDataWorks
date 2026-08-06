using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to deserialize REST response.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RestDeserializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RestDeserializationFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestDeserializationFailedCode"/> class.
    /// </summary>
    public RestDeserializationFailedCode()
        : base(91004, "RestDeserializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to deserialize response: {ErrorMessage}",
            isRetryable: false)
    {
    }
}