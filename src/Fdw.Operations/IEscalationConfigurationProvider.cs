using Fdw.Services.Abstractions;

namespace Fdw.Operations;

/// <summary>Supplies the configured EscalationPolicy members.</summary>
public interface IEscalationConfigurationProvider
    : IDomainConfigurationProvider<IEscalationPolicyImplementationConfiguration>
{
}
