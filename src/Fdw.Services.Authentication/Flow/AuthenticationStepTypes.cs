using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Binding;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.ServiceTypes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Every step a flow can name.
/// </summary>
/// <remarks>
/// <para>
/// One collection, whatever a step contributes. Splitting it per contribution would buy compile-time
/// typing and cost the open set — it breaks any step contributing two things, and forces a per-stage
/// configuration schema instead of the flat ordered list that keeps a flow readable. What a step
/// needs is checked when the flow loads instead, which catches the same mistakes earlier and by
/// name.
/// </para>
/// <para>
/// A package declaring a step and being referenced is what makes it selectable — there is no
/// registry to edit and no switch to extend. Removing the reference makes every flow naming that
/// step fail at startup rather than silently doing less than it used to.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>),
    typeof(IAuthenticationStepType),
    typeof(AuthenticationStepTypes),
    ServiceCategory = "AuthenticationStep")]
public partial class AuthenticationStepTypes : ServiceTypeCollectionBase<
    AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
    IAuthenticationStepType>
{
    /// <summary>
    /// Registers what a flow needs beyond the step options themselves.
    /// </summary>
    /// <remarks>
    /// The resolver, the flow provider and the runner are one registration each for the whole
    /// collection, so they belong here rather than in an application's startup — a host that had to
    /// call an AddXxx of its own is a host the next one has to remember to copy.
    /// </remarks>
    static AuthenticationStepTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            collectOptions?.Invoke(builder, loggerFactory);

            builder.Services.TryAddSingleton<IAcrPolicy, StandardAcrPolicy>();

            // What the shipped steps take beyond their own registration. A step is activated on
            // demand, so a dependency missing here fails at the first login that names the step,
            // never at startup - which is why these were found one deploy at a time.
            builder.Services.TryAddScoped<ITenantResolver, UserTenantResolver>();
            builder.Services.TryAddScoped<IIssuanceEligibility, UserAccountEligibility>();

            builder.Services.TryAddSingleton<IAuthenticationExecutionStore>(sp =>
                new InMemoryExecutionStore(sp.GetService<ILogger<InMemoryExecutionStore>>()));

            builder.Services.TryAddSingleton<IAuthorizationRequestStore>(sp =>
                new InMemoryAuthorizationRequestStore(
                    sp.GetService<ILogger<InMemoryAuthorizationRequestStore>>()));

            // The two providers AuthenticationFlowProvider reads through. Consumed via
            // GetRequiredService and registered nowhere, so the flow provider could not be built -
            // it surfaced as "No service for type ImplementationConfigurationProviderBase`2[...]"
            // only once everything ahead of it in startup had been fixed.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<
                AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<
                    AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<
                        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection, "auth"));

            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<
                AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<
                    AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<
                        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection, "auth"));

            // What the foreign-token exchange needs. A caller arriving on another authority's token
            // is bound to a local user through ExternalIdentity rows, so the binding reads its own
            // container the way every other implementation provider does.
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<
                ExternalIdentityConfiguration, ExternalIdentityConfigurationCommand>>(sp =>
                new ImplementationConfigurationProviderBase<
                    ExternalIdentityConfiguration, ExternalIdentityConfigurationCommand>(
                    sp.GetService<ILoggerFactory>()?.CreateLogger<ImplementationConfigurationProviderBase<
                        ExternalIdentityConfiguration, ExternalIdentityConfigurationCommand>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection, "auth"));

            builder.Services.TryAddScoped<IPrincipalBinding, ExternalIdentityBinding>();

            // The presented token comes off the request, so this follows the request scope.
            builder.Services.TryAddScoped<IForeignTokenAccessor, HttpForeignTokenAccessor>();

            // The code and state the provider's redirect carries back are also request-scoped, and
            // reading them off the query string is the same for every OIDC provider - only the
            // authority a flow sends the caller to is vendor-specific.
            builder.Services.TryAddScoped<IOidcCallbackAccessor, HttpOidcCallbackAccessor>();

            // Singleton because it caches the authority's keys across requests and refreshes them
            // on its own schedule; a scoped one would re-fetch per request and lose the point.
            builder.Services.TryAddSingleton<ISigningKeyProvider>(sp =>
                new CachingSigningKeyProvider(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CachingSigningKeyProvider)),
                    logger: sp.GetService<ILogger<CachingSigningKeyProvider>>()));

            builder.Services.TryAddSingleton<IAuthenticationFlowProvider>(sp =>
                new AuthenticationFlowProvider(
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>>(),
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>>(),
                    sp.GetService<ILogger<AuthenticationFlowProvider>>()));

            builder.Services.TryAddSingleton<IPasswordCredentialAccessor, HttpPasswordCredentialAccessor>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.TryAddScoped(sp => new AuthenticationRunner(
                sp.GetRequiredService<IAcrPolicy>(),
                sp.GetRequiredService<ITokenIssuer>(),
                sp.GetRequiredService<IAuthenticationExecutionStore>(),
                sp.GetRequiredService<IAuthenticationFlowProvider>(),
                // The collection IS the registry: a name resolves to the option that declared it.
                name => AuthenticationStepTypes.ByName(name) as IAuthenticationStep,
                sp.GetService<ILogger<AuthenticationRunner>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// PlatformConfiguration, because that is where a flow's DDL actually lives:
    /// <c>auth.AuthenticationFlow</c> and <c>auth.AuthenticationFlowStep</c> are declared in
    /// ConfigurationDb and seeded there. This previously said ServerConfiguration, on the argument
    /// that which providers a host accepts is the host's own business — a fair argument, and it
    /// contradicted the data model, so the flow provider read a store that held no flows.
    /// <para>
    /// Moving flows to ServerConfiguration would be a coherent design, but it is a schema change
    /// with a data migration behind it, not a connection-name edit. The trusted-issuer list is the
    /// part that genuinely is per-host, and that one does read from ServerConfiguration.
    /// </para>
    /// </remarks>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";
}
