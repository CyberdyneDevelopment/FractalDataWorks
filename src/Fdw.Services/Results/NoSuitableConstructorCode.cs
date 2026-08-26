using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// The service type does not expose the constructor the factory builds through.
/// </summary>
/// <remarks>
/// The factory constructs via <c>(ILogger&lt;TService&gt;, TConfiguration)</c>. A miss here is a
/// shape mismatch in the service type itself, not a configuration or registration problem — which
/// is why it is worth its own code: a caller seeing this needs to fix the type, and no amount of
/// re-resolving configuration will change the outcome.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "NoSuitableConstructor", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSuitableConstructorCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSuitableConstructorCode"/> class.
    /// </summary>
    public NoSuitableConstructorCode()
        : base(61004, "NoSuitableConstructor",
            ResultSeverities.ByName("Error"),
            "'{ServiceType}' has no constructor taking (ILogger<TService>, {ConfigurationType})",
            isRetryable: false)
    {
    }
}
