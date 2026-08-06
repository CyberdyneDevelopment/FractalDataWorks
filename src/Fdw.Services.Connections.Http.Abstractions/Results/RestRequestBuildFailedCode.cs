using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to build REST request.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "RestRequestBuildFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RestRequestBuildFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestRequestBuildFailedCode"/> class.
    /// </summary>
    public RestRequestBuildFailedCode()
        : base(91005, "RestRequestBuildFailed",
            ResultSeverities.ByName("Error"),
            "Failed to build REST request: {ErrorMessage}",
            isRetryable: false)
    {
    }
}