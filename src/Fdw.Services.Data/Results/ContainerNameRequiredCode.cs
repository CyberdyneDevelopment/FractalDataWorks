using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Container name was null or empty.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ContainerNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNameRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNameRequiredCode"/> class.
    /// </summary>
    public ContainerNameRequiredCode()
        : base(20000, "ContainerNameRequired", ResultSeverities.ByName("Error"),
            "Container name cannot be null or empty",
            isRetryable: false)
    {
    }
}