using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// The container's primary key has more than one column, and an OData resource is addressed by one id.
/// </summary>
/// <remarks>
/// A resource id in the request path names one value. Matching a composite key on its first column alone would address every
/// resource that shares that value, so the command is refused (FDW-6309).
/// </remarks>
[TypeOption(typeof(ODataResultCodes), "CompositePrimaryKeyNotSupported", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompositePrimaryKeyNotSupportedCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositePrimaryKeyNotSupportedCode"/> class.
    /// </summary>
    public CompositePrimaryKeyNotSupportedCode()
        : base(21004, "CompositePrimaryKeyNotSupported",
            ResultSeverities.ByName("Error"),
            "Container '{Container}' has a primary key of more than one column, and an OData resource is addressed by a single id",
            isRetryable: false)
    {
    }
}
