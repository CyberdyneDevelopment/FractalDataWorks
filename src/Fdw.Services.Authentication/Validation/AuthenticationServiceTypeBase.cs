using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.ServiceTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Base class for inbound-token validation mechanisms — the options of
/// <see cref="AuthenticationServiceTypes"/>.
/// </summary>
/// <remarks>
/// <para>
/// The collection reads the declared entries once, through the domain configuration provider, and
/// hands each to the option its <c>ServiceOptionType</c> names. A mechanism supplies only
/// <see cref="TakeScheme"/> — what a scheme for one entry actually is — and the binding it returns is
/// what lets <see cref="IssuerSchemeSelector"/> route to it.
/// </para>
/// <para>
/// Every option closes the base on the domain's one factory interface. An option's identity comes from
/// its name (<c>ServiceTypeBase.DeriveId</c>), not from its generic arguments, so a per-option factory
/// interface would distinguish nothing.
/// </para>
/// </remarks>
public abstract class AuthenticationServiceTypeBase
    : ServiceTypeBase<IGenericService, IAuthenticationValidationFactory, IServiceConfiguration>,
      IAuthenticationServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The mechanism name, matched against an entry's <c>ServiceOptionType</c>.</param>
    /// <param name="displayName">The display name for this option.</param>
    /// <param name="description">What this option validates.</param>
    protected AuthenticationServiceTypeBase(string name, string displayName, string description)
        : base(name,
               "AuthenticationServices",
               displayName,
               description,
               category: "Authentication",
               defaultDataStoreName: "ServerConfiguration",
               defaultPathName: "auth",
               defaultContainerName: "AuthenticationService")
    {
    }

    /// <inheritdoc />
    public abstract string[] SupportedProtocols { get; }

    /// <inheritdoc />
    public abstract string ProviderName { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedFlows { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedTokenTypes { get; }

    /// <inheritdoc />
    public abstract int Priority { get; }

    /// <inheritdoc />
    public abstract bool SupportsMultiTenant { get; }

    /// <inheritdoc />
    public abstract bool SupportsTokenCaching { get; }

    /// <summary>Takes an authentication scheme for one declared entry.</summary>
    /// <param name="configuration">The domain row, carrying the name, the kind and the authority.</param>
    /// <param name="schemes">The scheme provider the option adds its scheme to.</param>
    /// <param name="services">The built container, for anything the option needs to resolve.</param>
    /// <param name="loggerFactory">The logger factory, or null.</param>
    /// <remarks>
    /// Runs during Initialize, against the built container, because the entry it is given was read
    /// through a gateway. The option adds its scheme through <see cref="IAuthenticationSchemeProvider"/>
    /// rather than the builder, which is closed by this point.
    /// </remarks>
    public abstract IGenericResult<AuthenticationSchemeBinding> TakeScheme(
        IAuthenticationServiceConfiguration configuration,
        IAuthenticationSchemeProvider schemes,
        IServiceProvider services,
        ILoggerFactory? loggerFactory);
}
