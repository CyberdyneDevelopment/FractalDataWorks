using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// The OpenAPI generator failed to describe this host's endpoints.
/// </summary>
/// <remarks>
/// 90001, the non-retryable "operation failed" number: generation depends only on the build, so
/// asking again produces the same failure. The exception that caused it is in the log.
/// </remarks>
[TypeOption(typeof(ApiReferenceResultCodes), "DocumentGenerationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentGenerationFailedCode : ApiReferenceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentGenerationFailedCode"/> class.</summary>
    public DocumentGenerationFailedCode()
        : base(90001, "DocumentGenerationFailed",
            ResultSeverities.ByName("Error"),
            "The OpenAPI document '{documentName}' could not be generated: {reason}",
            isRetryable: false)
    {
    }
}
