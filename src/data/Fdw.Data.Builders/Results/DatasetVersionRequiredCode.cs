using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// The data set states no version and its kind declares none (FDW-3956). Nothing assumes one for it.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetVersionRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetVersionRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetVersionRequiredCode"/> class.
    /// </summary>
    public DatasetVersionRequiredCode()
        : base(21028, "DatasetVersionRequired",
            ResultSeverities.ByName("Error"),
            "Data set version is required",
            isRetryable: false)
    {
    }
}
