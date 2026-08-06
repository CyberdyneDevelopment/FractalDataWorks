using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Required parameter is missing.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParameterMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParameterMissingCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterMissingCode"/> class.
    /// </summary>
    public ParameterMissingCode()
        : base(21022, "ParameterMissing",
            ResultSeverities.ByName("Error"),
            "Required parameter '{ParameterName}' is missing",
            isRetryable: false)
    {
    }
}