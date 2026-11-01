using System;
using System.Net.Http;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// The factory for the JWT-assertion identity service.
/// </summary>
/// <remarks>
/// Its own type, so the container can tell this implementation's factory from the domain's others.
/// Both identity implementations declared the domain's closed generic, which is one service type
/// for two implementations -- the second to register is the one that disappears.
/// </remarks>
public interface IJwtAssertionIdentityFactory : IIdentityServiceFactory<IIdentityService, IIdentityServiceImplementationConfiguration>
{
}
