using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OpenApi.Results;

/// <summary>
/// Required parameter missing from command.
/// </summary>
[TypeOption(typeof(OpenApiResultCodes), "RequiredParameterMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequiredParameterMissingCode : OpenApiResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public RequiredParameterMissingCode()
        : base(20000, "RequiredParameterMissing",
            ResultSeverities.ByName("Error"),
            "Required parameter '{ParameterName}' not found in command",
            isRetryable: false)
    {
    }
}