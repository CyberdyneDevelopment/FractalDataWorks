using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Reads the foreign authority a flow's exchange step trusts.
/// </summary>
/// <remarks>
/// The step takes this rather than a configuration object, so the authority is read where every
/// other configuration is read and a host changing which authority it trusts changes a row rather
/// than a deployment.
/// </remarks>
public interface IForeignAuthorityConfigurationProvider
{

}
