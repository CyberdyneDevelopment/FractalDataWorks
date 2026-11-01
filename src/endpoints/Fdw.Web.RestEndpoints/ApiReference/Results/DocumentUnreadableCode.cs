using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.RestEndpoints.ApiReference.Results;

/// <summary>
/// The generated OpenAPI document is not one an API reference can be cut from.
/// </summary>
/// <remarks>
/// 90003, the "parse failed" number: the JSON did not parse, has no paths, or has an operation with
/// no record of who may call it. Serving it anyway would show a caller operations nobody checked
/// they may see, so the reference fails instead.
/// </remarks>
[TypeOption(typeof(ApiReferenceResultCodes), "DocumentUnreadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DocumentUnreadableCode : ApiReferenceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DocumentUnreadableCode"/> class.</summary>
    public DocumentUnreadableCode()
        : base(90003, "DocumentUnreadable",
            ResultSeverities.ByName("Error"),
            "The OpenAPI document cannot be cut into an API reference: {reason}",
            isRetryable: false)
    {
    }
}
