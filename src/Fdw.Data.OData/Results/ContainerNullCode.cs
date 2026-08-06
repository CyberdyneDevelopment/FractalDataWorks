using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Container parameter is null.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "ContainerNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerNullCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerNullCode"/> class.
    /// </summary>
    public ContainerNullCode()
        : base(20000, "ContainerNull",
            ResultSeverities.ByName("Error"),
            "Container cannot be null",
            isRetryable: false)
    {
    }
}