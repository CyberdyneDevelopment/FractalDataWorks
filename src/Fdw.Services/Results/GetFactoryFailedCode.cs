using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Failed to get factory.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "GetFactoryFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GetFactoryFailedCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetFactoryFailedCode"/> class.
    /// </summary>
    public GetFactoryFailedCode()
        : base(70001, "GetFactoryFailed",
            ResultSeverities.ByName("Error"),
            "Failed to get factory",
            isRetryable: false)
    {
    }
}