using System.Collections.Generic;

namespace Fdw.Services.Abstractions;

/// <summary>
/// A service type that declares services it needs registered (FDW-4588); its collection checks them at Initialize.
/// </summary>
/// <remarks>
/// Why a separate interface and not a member of <see cref="Fdw.Collections.IServiceTypeRegistration"/>: endpoint collections implement that one too, and a
/// declaration of requirements is only meaningful for a service type.
/// </remarks>
public interface IServiceRequirements
{
    /// <summary>Gets the services this service type needs registered.</summary>
    IReadOnlyList<ServiceRequirement> Requirements { get; }
}
