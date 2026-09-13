using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.ServiceTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Collection of inbound-token validation mechanisms — the ways this host can establish who is
/// calling it.
/// </summary>
/// <remarks>
/// <para>
/// A host holds as many of these at once as it has issuers to trust. A resource server reached both by
/// a person's token from its own auth server and by a scheduled dispatch's token from a corporate IdP
/// has two, and each token is validated by the one that was declared for its issuer — not by a single
/// validator taught to accept both, which is how the trust boundary of one issuer leaks onto another.
/// </para>
/// <para>
/// This collection registers what is common to that arrangement: the policy scheme that routes a
/// request to a mechanism, and the scheme that fails a request whose issuer nothing declared. The
/// mechanisms themselves are its options.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(AuthenticationServiceTypeBase),
    typeof(IAuthenticationServiceType),
    typeof(AuthenticationServiceTypes),
    ServiceCategory = "Authentication")]
public partial class AuthenticationServiceTypes : ServiceTypeCollectionBase<AuthenticationServiceTypeBase, IAuthenticationServiceType>
{
    /// <summary>The connection these rows are read through.</summary>
    /// <remarks>
    /// ServerConfiguration, not PlatformConfiguration: which issuers a host trusts is server
    /// configuration. Two hosts sharing a tenant legitimately trust different issuers, the same
    /// reason the flows themselves live there.
    /// </remarks>
    public static string ConfigurationConnection { get; set; } = "ServerConfiguration";

    /// <summary>The ServerConfiguration folder this domain's rows live in.</summary>
    public static string ServerConfigurationPath { get; set; } = "auth";

    // Configure(), Register() and Initialize() are source-generated. Each body below runs the loop
    // over this collection's options first - which is what Registration and Initialization replace -
    // then the part this domain owns for the whole collection rather than per option.
    static AuthenticationServiceTypes()
    {
        var collectOptions = RegisterFunc;
        var initializeOptions = InitializationFunc;

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<AuthenticationServiceTypes>()
                ?? NullLogger<AuthenticationServiceTypes>.Instance;

            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            if (Options.Length == 0)
            {
                AuthenticationValidationLog.NoMechanismsRegistered(log);
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            }

            // The domain provider. Which issuers this host trusts is server configuration, read
            // through the gateway onto the store the host declared it on — not application settings,
            // because two hosts sharing a tenant legitimately trust different issuers, the same
            // reason the flows themselves live there.
            builder.Services.AddSingleton<IAuthenticationServiceConfigurationProvider, AuthenticationServiceConfigurationProvider>(sp => new AuthenticationServiceConfigurationProvider(sp.GetRequiredService<ILogger<AuthenticationServiceConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationServiceTypes.ConfigurationConnection));

            // Filled during Initialize, once the gateway can be reached. The selector reads it per
            // request, so it cannot be a set of service registrations made before the container is
            // built — the configuration that decides its contents is not readable yet.
            builder.Services.TryAddSingleton<AuthenticationSchemeBindings>();

            // The routing this domain owns, which does not depend on what is configured: an
            // unmatched issuer is refused, and everything else is forwarded by the selector.
            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, UnmatchedIssuerHandler>(
                    UnmatchedIssuerHandler.SchemeName, displayName: null, configureOptions: _ => { })
                .AddPolicyScheme(
                    IssuerSchemeSelector.SchemeName,
                    displayName: null,
                    configureOptions: options => options.ForwardDefaultSelector = IssuerSchemeSelector.Select);

            builder.Services.AddSingleton<IPostConfigureOptions<AuthenticationOptions>, SelectorIsDefaultScheme>();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Initialize and not Register: the entries are read through a gateway, which exists only once
        // the container is built. Nothing needs them earlier - the first token to arrive is what needs
        // a scheme to route to, and that is a request, long after this.
        Initialization((host, hostLoggerFactory) =>
        {
            var log = hostLoggerFactory?.CreateLogger<AuthenticationServiceTypes>()
                ?? NullLogger<AuthenticationServiceTypes>.Instance;

            var initialized = initializeOptions(host, hostLoggerFactory);
            if (initialized.IsFailure)
                return initialized;

            if (Options.Length == 0)
                return GenericResult<IHost>.Success(host);

            var services = host.Services;

            // The elevation is opened and closed around this read, by the domain that needs it. The
            // read has no ClaimsPrincipal and no TenantId, so under the RLS predicates it resolves to
            // the deny-everywhere principal and returns nothing -- the host then cannot see its own
            // declared authentication services. Scoping it here rather than in the host keeps the
            // window to the one statement that needs it, and means no host has to know to do it.
            IGenericResult<IReadOnlyList<IAuthenticationServiceImplementationConfiguration>> declared;
            using (new SystemAuthenticationContextScope(
                services.GetRequiredService<IAuthenticationContextAccessor>()))
            {
                // VSTHRD002: Initialization is a synchronous phase by contract, and this runs once at
                // startup on the host's own thread before any request exists to deadlock against.
#pragma warning disable VSTHRD002
                declared = services.GetRequiredService<IAuthenticationServiceConfigurationProvider>()
                    .Get(CancellationToken.None)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            }

            if (!declared.IsSuccess || declared.Value is null)
                return declared.ToNewResult<IHost>();

            var bindings = services.GetRequiredService<AuthenticationSchemeBindings>();

            foreach (var entry in declared.Value)
            {
                if (!entry.Enabled)
                    continue;

                if (entry.Name is not { Length: > 0 } serviceName)
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));

                // Through IssuerName, which the options bridge also reads its ValidIssuer through:
                // the selector matches the binding ordinally and the scheme checks ValidIssuer
                // ordinally, so both have to derive from the same rule or a declared "https://host"
                // routes a token minted against "https://host/" and is then refused by the scheme
                // it was correctly routed to.
                var issuer = IssuerName.Read(entry.Authority, serviceName, log);
                if (!issuer.IsSuccess || issuer.Value is null)
                    return issuer.ToNewResult<IHost>();

                entry.Authority = issuer.Value;

                // Which option serves this row is the row's own Implementation -- the same name the
                // option registered its implementation provider under, stamped onto the row by the
                // domain provider during dispatch. Matching on the configuration TYPE could never
                // work: ConfigurationType is ServiceTypeBase's TConfiguration, and every option here
                // closes it to IServiceConfiguration, so all of them compared equal to each other and
                // none to the concrete row. Every declared service was reported as an unreadable
                // section no matter how correctly it was configured.
                if (ByName(entry.Implementation) is not AuthenticationServiceTypeBase option
                    || option == NotFound)
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.NoOptionForImplementation(
                            log, serviceName, entry.Implementation));

                var kind = option.Name;

                var binding = option.TakeScheme(entry, services.GetRequiredService<IAuthenticationSchemeProvider>(), services, hostLoggerFactory);
                if (!binding.IsSuccess || binding.Value is null)
                    return binding.ToNewResult<IHost>();

                bindings.Add(binding.Value);
                AuthenticationValidationLog.SchemeRegistered(
                    log, binding.Value.ServiceName, kind, binding.Value.SchemeName, binding.Value.Issuer);
            }

            if (bindings.Count == 0)
                return GenericResult<IHost>.Failure(
                    AuthenticationValidationLog.NoAuthenticationServicesDeclared(log, "AuthenticationServices"));

            AuthenticationValidationLog.RoutingRegistered(log, bindings.Count, IssuerSchemeSelector.SchemeName);
            return GenericResult<IHost>.Success(host);
        });
    }

    private sealed class SelectorIsDefaultScheme : IPostConfigureOptions<AuthenticationOptions>
    {
        public void PostConfigure(string? name, AuthenticationOptions options)
        {
            options.DefaultScheme = IssuerSchemeSelector.SchemeName;
            options.DefaultAuthenticateScheme = IssuerSchemeSelector.SchemeName;
            options.DefaultChallengeScheme = IssuerSchemeSelector.SchemeName;
        }
    }
}
