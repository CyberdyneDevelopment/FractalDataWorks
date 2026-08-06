using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container was not found in configuration.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerNotFoundInConfiguration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNotFoundInConfigurationCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNotFoundInConfigurationCode"/> class.
    /// </summary>
    public ContainerNotFoundInConfigurationCode()
        : base(31000, "ContainerNotFoundInConfiguration", ResultSeverities.ByName("Error"),
            "Container '{ContainerName}' not found in configuration",
            isRetryable: false)
    {
    }
}