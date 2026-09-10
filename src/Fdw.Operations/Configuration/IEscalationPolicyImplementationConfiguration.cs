using Fdw.Configuration;

namespace Fdw.Operations.Configuration;

/// <summary>
/// The contract every EscalationPolicy implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered
/// against this domain or handed back by a read of it.
/// </remarks>
public interface IEscalationPolicyImplementationConfiguration : IImplementationConfiguration
{
}
