using System;
using Fdw.Collections;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// A discriminator option for the <c>AegisCommandConfiguration.ServiceOptionType</c> column,
/// resolving to the .NET type of the corresponding typed-body approval-policy configuration.
/// </summary>
/// <remarks>
/// Mirrors how <c>IConnectionType.ConfigurationType</c> lets <c>ConnectionConfigurationJsonConverter</c>
/// dispatch without a hardcoded type-name switch. This is config-data, not a DI-resolved service, so
/// it lives on an open <c>[MutableTypeCollection]</c> (see <see cref="ApprovalPolicyTypes"/>) rather
/// than a <c>ServiceTypeCollection</c>.
/// </remarks>
public interface IApprovalPolicyType : ITypeOption<int, ApprovalPolicyTypeBase>
{
    /// <summary>Gets the .NET type of the typed-body configuration for this policy kind.</summary>
    Type ConfigurationType { get; }
}
