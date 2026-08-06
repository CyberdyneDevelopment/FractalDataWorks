using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No factory registered for a service option type.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "NoFactoryRegistered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoFactoryRegisteredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFactoryRegisteredCode"/> class.
    /// </summary>
    public NoFactoryRegisteredCode()
        : base(61002, "NoFactoryRegistered",
            ResultSeverities.ByName("Error"),
            "No factory registered for service option type '{ServiceOptionType}'",
            isRetryable: false)
    {
    }
}