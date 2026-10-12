using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Unknown security type specified.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "UnknownSecurityType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnknownSecurityTypeCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownSecurityTypeCode"/> class.
    /// </summary>
    public UnknownSecurityTypeCode()
        : base(90004, "UnknownSecurityType",
            ResultSeverities.ByName("Error"),
            "Unknown security type: {SecurityType}",
            isRetryable: false)
    {
    }
}