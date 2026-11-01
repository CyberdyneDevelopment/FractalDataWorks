using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.Identity;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Fdw.Services.Identity.ClientCredentials;

/// <summary>
/// The ClientCredentialsIdentity implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IClientCredentialsIdentityFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IClientCredentialsIdentityProvider
    : IImplementationServiceProvider<IIdentityService, IIdentityServiceImplementationConfiguration>
{
}
