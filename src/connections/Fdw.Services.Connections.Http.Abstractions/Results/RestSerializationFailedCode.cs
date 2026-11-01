using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to serialize REST request body.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RestSerializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RestSerializationFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestSerializationFailedCode"/> class.
    /// </summary>
    public RestSerializationFailedCode()
        : base(91006, "RestSerializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to serialize request body: {ErrorMessage}",
            isRetryable: false)
    {
    }
}