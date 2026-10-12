using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Dataset name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "DatasetNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatasetNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatasetNameRequiredCode"/> class.
    /// </summary>
    public DatasetNameRequiredCode()
        : base(21005, "DatasetNameRequired",
            ResultSeverities.ByName("Error"),
            "Dataset name is required",
            isRetryable: false)
    {
    }
}