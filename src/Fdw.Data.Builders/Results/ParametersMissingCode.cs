using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Required parameters are missing.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "ParametersMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ParametersMissingCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParametersMissingCode"/> class.
    /// </summary>
    public ParametersMissingCode()
        : base(21021, "ParametersMissing",
            ResultSeverities.ByName("Error"),
            "Required parameters missing: {RequiredParameters}",
            isRetryable: false)
    {
    }
}