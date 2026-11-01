using Fdw.Operations.Configuration;
using Fdw.Services.Abstractions;

namespace Fdw.Operations;

/// <summary>Supplies the EscalationPolicy implementation's own configuration to the domain that registers it.</summary>
public interface IEscalationPolicyImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IEscalationPolicyImplementationConfiguration>
{
}
