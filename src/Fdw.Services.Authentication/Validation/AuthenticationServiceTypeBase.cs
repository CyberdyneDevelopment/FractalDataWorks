using System.Collections.Generic;
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
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "auth",
               defaultContainerName: "Authentication")
    {
        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<AuthenticationServiceTypes>()
                ?? NullLogger<AuthenticationServiceTypes>.Instance;

            var declared = AuthenticationServiceConfiguration.Read(builder.Configuration, Name, log);
            // Why the read's own reason travels rather than a restatement: it names which entry and
            // which field, and a caller told only "authentication configuration is invalid" has to go
            // find that out again.
            if (declared.IsFailure)
                return declared.ToNewResult<IHostApplicationBuilder>();
            if (declared.Value is not { } entries)
                return GenericResult<IHostApplicationBuilder>.Failure(
                    AuthenticationValidationLog.SectionUnreadable(log, Name));

            // Why called even with no entries: it is what brings the ASP.NET authentication services
            // into the container, and the collection's own Register adds the selector scheme through
            // the same builder.
            var authenticationBuilder = builder.Services.AddAuthentication();

            foreach (var (header, section) in entries)
            {
                var binding = RegisterScheme(authenticationBuilder, header, section, loggerFactory);
                if (binding.IsFailure)
                    return binding.ToNewResult<IHostApplicationBuilder>();
                if (binding.Value is not { } registered)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SchemeNotProduced(log, header.Name ?? section.Path, Name));

                builder.Services.AddSingleton(registered);
                AuthenticationValidationLog.SchemeRegistered(
                    log, registered.ServiceName, Name, registered.SchemeName, registered.Issuer);
            }

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
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
    /// <param name="loggerFactory">The host's logger factory, if it has one.</param>
    /// <returns>
    /// The issuer/scheme binding this entry contributes. Failure when the entry's typed body is
    /// incomplete — reported with the reason, never with a scheme built on assumed values.
    /// </returns>
    public abstract IGenericResult<AuthenticationSchemeBinding> RegisterScheme(
        AuthenticationBuilder authenticationBuilder,
        AuthenticationServiceConfiguration configuration,
        Microsoft.Extensions.Configuration.IConfigurationSection section,
        ILoggerFactory? loggerFactory);
}
