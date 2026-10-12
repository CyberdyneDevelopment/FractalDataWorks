using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No factory registered for service type.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "FactoryNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FactoryNotFoundCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryNotFoundCode"/> class.
    /// </summary>
    public FactoryNotFoundCode()
        : base(60002, "FactoryNotFound",
            ResultSeverities.ByName("Error"),
            "No factory registered for service type '{TypeName}'. Available types: {AvailableTypes}",
            isRetryable: false)
    {
    }
}