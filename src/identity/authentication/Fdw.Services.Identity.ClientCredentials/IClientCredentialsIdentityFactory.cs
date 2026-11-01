using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// The factory for the client-credentials identity service.
/// </summary>
/// <remarks>
/// Its own type, so the container can tell this implementation's factory from the domain's others.
/// Both identity implementations declared the domain's closed generic, which is one service type
/// for two implementations -- the second to register is the one that disappears.
/// </remarks>
public interface IClientCredentialsIdentityFactory : IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>
{
}
