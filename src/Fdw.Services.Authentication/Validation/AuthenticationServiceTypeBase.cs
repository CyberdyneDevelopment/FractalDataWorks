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
/// The Register body is written once, here, because it is the same for every mechanism: read the
/// host's <c>AuthenticationServices</c> entries that name this option, add a scheme for each, and
/// register the binding that lets <see cref="IssuerSchemeSelector"/> route to it. A mechanism supplies
/// only <see cref="RegisterScheme"/> — what a scheme for one entry actually is.
/// </para>
/// <para>
/// A derived option that needs more of the Register phase appends to this one rather than setting it:
/// <c>Registration</c> assigns, discarding this body along with the entries it would have read.
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
               AuthenticationServiceConfiguration.SectionName,
               displayName,
               description,
               category: "Authentication",
               defaultDataStoreName: "PlatformConfiguration",
               defaultPathName: "auth",
               defaultContainerName: "Authentication")
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

    /// <summary>
    /// Adds the authentication scheme for one declared entry and reports what it added.
    /// </summary>
    /// <param name="authenticationBuilder">The host's authentication builder.</param>
    /// <param name="configuration">The entry's header, already validated.</param>
    /// <param name="section">The configuration section the entry was read from, for this option's typed body.</param>
    /// <param name="services">
    /// The collection the scheme's own dependencies are registered in. Registration runs before the
    /// container is built, so an option needing a service at validation time registers what it needs
    /// here and resolves it from the request rather than holding process-wide state.
    /// </param>
    /// <param name="loggerFactory">The host's logger factory, if it has one.</param>
    /// <returns>
    /// The issuer/scheme binding this entry contributes. Failure when the entry's typed body is
    /// incomplete — reported with the reason, never with a scheme built on assumed values.
    /// </returns>
    public abstract IGenericResult<AuthenticationSchemeBinding> RegisterScheme(
        AuthenticationBuilder authenticationBuilder,
        AuthenticationServiceConfiguration configuration,
        Microsoft.Extensions.Configuration.IConfigurationSection section,
        IServiceCollection services,
        ILoggerFactory? loggerFactory);
}
