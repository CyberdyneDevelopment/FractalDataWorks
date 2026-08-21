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

            // Why the collection can legitimately have no options: this assembly carries the domain, and
            // a host that references it for anything else picks the domain up with it. A domain with no
            // mechanism has nothing to route between and registers nothing.
            if (Options.Length == 0)
            {
                AuthenticationValidationLog.NoMechanismsRegistered(log);
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            }

            // Why the collection registers the schemes and AuthenticationServiceTypeBase no longer does:
            // reading the declared entries and turning each into a scheme is the same procedure for every
            // mechanism - only RegisterScheme differs, and that is already a public abstract on the option.
            // Run from the base it occupied each option's own phase body, so a derived option could not
            // state its own registration without silently discarding it (STC002).
            //
            // Why AddAuthentication() is called even when a mechanism declares no entries: it is what
            // brings the ASP.NET authentication services into the container, and the selector scheme below
            // is added through the same builder.
            var authenticationBuilder = builder.Services.AddAuthentication();

            foreach (var option in Options)
            {
                // Why this fails loud: Options is IServiceTypeRegistration[], and an option in this
                // collection that is not a mechanism cannot produce a scheme. Skipping it would leave the
                // host trusting fewer issuers than it declared, which surfaces only as tokens being
                // rejected at runtime with nothing naming the mechanism that never registered.
                if (option is not AuthenticationServiceTypeBase mechanism)
                    return GenericResult<IHostApplicationBuilder>.Failure(
                        AuthenticationValidationLog.SectionUnreadable(log, option.Name));

                // Why the read's own reason travels rather than a restatement: it names which entry and
                // which field, and a caller told only "authentication configuration is invalid" has to go
                // find that out again.
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

            // Why the descriptor scan: each option registered one binding per entry it read, and the
            // count is the only statement of how many issuers this host ended up trusting. Building a
            // second container to ask would be worse than counting descriptors.
            var bindings = builder.Services.Count(d => d.ServiceType == typeof(AuthenticationSchemeBinding));

            // Why a failure and not a quiet skip: a mechanism IS registered, so this host means to
            // validate tokens, and no issuer was declared for it to trust. Every protected route would
            // then reject everything, and doing that silently reads at runtime as a token problem rather
            // than the missing section it is.
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

            // Why post-configure rather than another AddAuthentication(o => ...): the default scheme is
            // also set by whichever mechanisms register one of their own, and Configure delegates run in
            // registration order — which is option-discovery order, so the winner would depend on which
            // package happened to be loaded last. Post-configure runs after all of them, so the request
            // is routed by issuer no matter what order the mechanisms arrived in.
            builder.Services.AddSingleton<IPostConfigureOptions<AuthenticationOptions>, SelectorIsDefaultScheme>();

            AuthenticationValidationLog.RoutingRegistered(log, bindings, IssuerSchemeSelector.SchemeName);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    // Why a named type rather than PostConfigure<T>(...): the lambda overload registers an
    // IPostConfigureOptions per call and gives the reader nothing to look up when the default scheme
    // turns out not to be what they expected.
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
