using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
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

            foreach (var option in Options)
            {
                if (option is not AuthenticationServiceTypeBase mechanism)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, option.Name));

                var declared = AuthenticationServiceConfiguration.Read(builder.Configuration, mechanism.Name, log);
                if (declared.IsFailure)
                    return declared.ToNewResult<IHostApplicationBuilder>();
                if (declared.Value is not { } entries)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, mechanism.Name));

                foreach (var (header, section) in entries)
                {
                    var binding = mechanism.RegisterScheme(authenticationBuilder, header, section, loggerFactory);
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
