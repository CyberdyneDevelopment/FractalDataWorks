using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Fdw.ServiceTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
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

            var authenticationBuilder = builder.Services.AddAuthentication();

            // Which issuers this host trusts is server configuration, not application settings, so
            // it is read from the ServerConfiguration store rather than from builder.Configuration.
            // Two hosts sharing a tenant legitimately trust different issuers, which is the same
            // reason the flows themselves live there.
            var serverConfiguration = ServerConfigurationStore.Read(
                SchemaFileName,
                ServerConfigurationPath,
                ServerConfigurationTable,
                AuthenticationServiceConfiguration.SectionName);

            foreach (var option in Options)
            {
                if (option is not AuthenticationServiceTypeBase mechanism)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, option.Name));

                var declared = AuthenticationServiceConfiguration.Read(serverConfiguration, mechanism.Name, log);
                if (declared.IsFailure)
                    return declared.ToNewResult<IHostApplicationBuilder>();
                if (declared.Value is not { } entries)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, mechanism.Name));

                foreach (var (header, section) in entries)
                {
                    var binding = mechanism.RegisterScheme(
                        authenticationBuilder, header, section, builder.Services, loggerFactory);
                    if (binding.IsFailure)
                        return binding.ToNewResult<IHostApplicationBuilder>();
                    if (binding.Value is not { } scheme)
                        return GenericResult<IHostApplicationBuilder>.Failure(
                            AuthenticationValidationLog.SchemeNotProduced(log, header.Name ?? section.Path, mechanism.Name));

                    builder.Services.AddSingleton(scheme);
                    AuthenticationValidationLog.SchemeRegistered(
                        log, scheme.ServiceName, mechanism.Name, scheme.SchemeName, scheme.Issuer);
                }
            }

            var bindings = builder.Services.Count(d => d.ServiceType == typeof(AuthenticationSchemeBinding));

            if (bindings == 0)
                return GenericResult<IHostApplicationBuilder>.Failure(
                    AuthenticationValidationLog.NoAuthenticationServicesDeclared(
                        log, AuthenticationServiceConfiguration.SectionName));

            builder.Services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, UnmatchedIssuerHandler>(
                    UnmatchedIssuerHandler.SchemeName, displayName: null, configureOptions: _ => { })
                .AddPolicyScheme(
                    IssuerSchemeSelector.SchemeName,
                    displayName: null,
                    configureOptions: options => options.ForwardDefaultSelector = IssuerSchemeSelector.Select);

            builder.Services.AddSingleton<IPostConfigureOptions<AuthenticationOptions>, SelectorIsDefaultScheme>();

            AuthenticationValidationLog.RoutingRegistered(log, bindings, IssuerSchemeSelector.SchemeName);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
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
