using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Http.Results;

/// <summary>
/// Required path parameter is missing.
/// </summary>
[TypeOption(typeof(DataHttpResultCodes), "RequiredParameterMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequiredParameterMissingCode : DataHttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredParameterMissingCode"/> class.
    /// </summary>
    public RequiredParameterMissingCode()
        : base(20000, "RequiredParameterMissing",
            ResultSeverities.ByName("Error"),
            "Required parameter '{ParameterName}' is missing",
            isRetryable: false)
    {
    }
}