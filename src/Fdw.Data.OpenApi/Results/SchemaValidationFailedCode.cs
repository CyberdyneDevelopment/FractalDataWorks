using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// Schema validation failed.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "SchemaValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SchemaValidationFailedCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public SchemaValidationFailedCode()
        : base(20001, "SchemaValidationFailed",
            ResultSeverities.ByName("Error"),
            "Schema validation failed for '{PropertyName}': {Error}",
            isRetryable: false)
    {
    }
}