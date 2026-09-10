using Fdw.Configuration;

namespace Fdw.Services.Tests.Configuration;

/// <summary>
/// The contract every TestBody implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// </remarks>
public interface ITestBodyImplementationConfiguration : IImplementationConfiguration
{
}
