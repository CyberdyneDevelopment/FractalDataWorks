using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Logging;
using Fdw.Services.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System;

namespace Fdw.Services.Identity.JwtAssertion;

/// <summary>
/// The JwtAssertionIdentity implementation of its domain.
/// </summary>
/// <remarks>
/// A per-option interface for the same reason <c>IJwtAssertionIdentityFactory</c> has one: each implementation
/// closes its base with its OWN interface, and that is what the domain provider registers against.
/// </remarks>
public interface IJwtAssertionIdentityProvider
    : IImplementationServiceProvider<IIdentityService, IIdentityServiceImplementationConfiguration>
{
}
