using Fdw.Aegis.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.Aegis;

/// <summary>
/// Supplies the declared Aegis commands. Registered and resolved as this type -- never as the base.
/// </summary>
public interface IAegisCommandConfigurationProvider
    : IDomainConfigurationProvider<IApprovalPolicyConfiguration>
{
}
