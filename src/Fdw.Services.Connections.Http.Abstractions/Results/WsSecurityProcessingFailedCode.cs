using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// WS-Security processing failed.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "WsSecurityProcessingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WsSecurityProcessingFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WsSecurityProcessingFailedCode"/> class.
    /// </summary>
    public WsSecurityProcessingFailedCode()
        : base(91012, "WsSecurityProcessingFailed",
            ResultSeverities.ByName("Error"),
            "WS-Security processing failed: {ErrorMessage}",
            isRetryable: false)
    {
    }
}