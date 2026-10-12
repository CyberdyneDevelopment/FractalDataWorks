using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Factory does not implement required interface.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "InvalidFactoryType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidFactoryTypeCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFactoryTypeCode"/> class.
    /// </summary>
    public InvalidFactoryTypeCode()
        : base(61000, "InvalidFactoryType",
            ResultSeverities.ByName("Error"),
            "Factory for service type '{TypeName}' does not implement {RequiredInterface}",
            isRetryable: false)
    {
    }
}