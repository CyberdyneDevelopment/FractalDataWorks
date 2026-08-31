using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Linq;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.ServiceTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    /// <summary>The bootstrap schema declaring where ServerConfiguration lives.</summary>
    /// <remarks>
    /// Settable for the same reason <c>ConfigurationGatewayTypes.SchemaFileName</c> is: a host that
    /// ships its schema under another name says so once, here, rather than everywhere it is read.
    /// </remarks>
    /// <summary>The connection these rows are read through.</summary>
    /// <remarks>
    /// ServerConfiguration, not PlatformConfiguration: which issuers a host trusts is server
    /// configuration. Two hosts sharing a tenant legitimately trust different issuers, the same
    /// reason the flows themselves live there.
    /// </remarks>
    public static string ConfigurationConnection { get; set; } = "ServerConfiguration";

    /// <summary>The schema file the host declares its connections in.</summary>
    public static string SchemaFileName { get; set; } = "configurationSchema.json";

    /// <summary>The ServerConfiguration folder this domain's rows live in.</summary>
    public static string ServerConfigurationPath { get; set; } = "auth";

    /// <summary>The file, without extension, holding the declared authentication services.</summary>
    // Singular, matching the table it mirrors. The section it is exposed under is plural because
    // that is the name its reader has always used, and renaming a configuration key to match a file
    // name would break every host that already declares one.
    public static string ServerConfigurationTable { get; set; } = "AuthenticationService";

    // Configure(), Register(), Initialize() are source-generated. This replaces Register's body with
    // the option collect followed by the routing this domain owns — written beside the declaration
    // because it is one registration for the whole collection, not per option.
    static AuthenticationServiceTypes()
    {
        var collectOptions = RegisterFunc;

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
            builder.Services.TryAddSingleton<AuthenticationServiceConfigurationProvider>(sp =>
                new AuthenticationServiceConfigurationProvider(
                    sp.GetRequiredService<ILogger<AuthenticationServiceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection,
                    ServerConfigurationPath));
            builder.Services.TryAddSingleton<IAuthenticationServiceConfigurationProvider>(sp =>
                sp.GetRequiredService<AuthenticationServiceConfigurationProvider>());

            // Filled during Initialize, once the gateway can be reached. The selector reads it per
            // request, so it cannot be a set of service registrations made before the container is
            // built — the configuration that decides its contents is not readable yet.
            builder.Services.TryAddSingleton<AuthenticationSchemeBindings>();

            // The adapter between JwtBearerHandler's options contract and the configuration system.
            // IConfigureOptions is the service type OptionsFactory resolves; registered under the
            // named interface it would sit in a collection nothing reads.
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>(sp =>
                new ConfigureLocalKeyScheme(sp));

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

        // Initialize, not Register: the entries are read through a gateway, and a gateway exists only
        // once the container is built. Nothing needs them earlier — the first token to arrive is what
        // needs a scheme to route to, and that is a request, long after this.
        Initialization((host, hostLoggerFactory) =>
        {
            var log = hostLoggerFactory?.CreateLogger<AuthenticationServiceTypes>()
                ?? NullLogger<AuthenticationServiceTypes>.Instance;

            if (Options.Length == 0)
                return GenericResult<IHost>.Success(host);

            var services = host.Services;
            // VSTHRD002: Initialization is a synchronous phase by contract, and this runs once at
            // startup on the host's own thread before any request exists to deadlock against.
#pragma warning disable VSTHRD002
            var declared = services.GetRequiredService<IAuthenticationServiceConfigurationProvider>()
                .GetHeaders(CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

            if (!declared.IsSuccess || declared.Value is null)
                return declared.ToNewResult<IHost>();

            var schemes = services.GetRequiredService<IAuthenticationSchemeProvider>();
            var bindings = services.GetRequiredService<AuthenticationSchemeBindings>();

            foreach (var entry in declared.Value)
            {
                if (!entry.Enabled)
                    continue;

                if (entry.Name is not { Length: > 0 } serviceName)
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.EntryMissingName(log, "(unnamed)"));

                if (entry.ServiceOptionType is not { Length: > 0 } kind)
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, serviceName));

                // The issuer a token carries is what routes it, and the selector matches it
                // ordinally, so a declared "https://host" must become "https://host/" here or it
                // never matches a token minted against it. Normalising once, where the entry is
                // read, is what keeps every option and the selector comparing the same string.
                if (!Uri.TryCreate(entry.Authority, UriKind.Absolute, out var authority)
                    || (!string.Equals(authority.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
                        && !string.Equals(authority.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)))
                {
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.AuthorityNotAbsolute(
                            log, serviceName, entry.Authority ?? string.Empty));
                }

                entry.Authority = authority.AbsoluteUri;

                if (ByName(kind) is not AuthenticationServiceTypeBase option)
                    return GenericResult<IHost>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, kind));

                var binding = option.TakeScheme(entry, schemes, services, hostLoggerFactory);
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
