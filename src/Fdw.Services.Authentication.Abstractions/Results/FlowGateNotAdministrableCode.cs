using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// The flow is limited by a gate step this host cannot configure.
/// </summary>
/// <remarks>
/// The gate does not implement IAdministrablePermissionGate, so its settings are not this service's to write. Opening the flow still works; changing its permission is done in configuration. An endpoint answers it with a 409.
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "FlowGateNotAdministrable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FlowGateNotAdministrableCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="FlowGateNotAdministrableCode"/> class.</summary>
    public FlowGateNotAdministrableCode()
        : base(41004, "FlowGateNotAdministrable",
            ResultSeverities.ByName("Error"),
            "The flow's permission gate is changed in configuration, not here",
            isRetryable: false)
    {
    }
}
