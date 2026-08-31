using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// A domain's operational connection was never named by the host.
/// </summary>
/// <remarks>
/// Category 6, configuration and setup: it fails fast at boot rather than at the first read. A
/// domain's operational store is a row INSIDE the configuration store, so unlike the configuration
/// connection there is no name the framework can supply — a default here would name a store the
/// application only hopes exists, which is the absence the no-fallbacks rule exists to catch.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "OperationalConnectionNotSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OperationalConnectionNotSetCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationalConnectionNotSetCode"/> class.
    /// </summary>
    public OperationalConnectionNotSetCode()
        : base(61006, "OperationalConnectionNotSet",
            ResultSeverities.ByName("Error"),
            "Operational connection for '{Domain}' is not set; the host must set {Domain}.{Property} before registration",
            isRetryable: false)
    {
    }
}
