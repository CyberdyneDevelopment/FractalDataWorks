using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Binding;
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

            // Scoped, not singleton: it resolves steps from the scope it was built in, and steps
            // read through providers that are themselves scoped. The name-to-type map it consults
            // is static, so scoping the resolver costs a lookup rather than a re-registration.
            builder.Services.TryAddScoped<AuthenticationStepResolver>(sp =>
                new AuthenticationStepResolver(sp, sp.GetService<ILogger<AuthenticationStepResolver>>()));

            builder.Services.TryAddScoped<IAuthenticationStepResolver>(sp =>
                sp.GetRequiredService<AuthenticationStepResolver>());

            builder.Services.TryAddSingleton<IAcrPolicy, StandardAcrPolicy>();

            // PasswordCredentialStep takes this, so an unregistered resolver is not a missing
            // feature - it is a step that cannot be activated, and the flow fails at the login
            // rather than at startup.
            builder.Services.TryAddScoped<ITenantResolver, UserTenantResolver>();

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

            builder.Services.TryAddSingleton<IAuthenticationFlowProvider>(sp =>
                new AuthenticationFlowProvider(
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowConfiguration, AuthenticationFlowConfigurationCommand>>(),
                    sp.GetRequiredService<ImplementationConfigurationProviderBase<
                        AuthenticationFlowStepConfiguration, AuthenticationFlowStepConfigurationCommand>>(),
                    sp.GetRequiredService<AuthenticationStepResolver>(),
                    sp.GetService<ILogger<AuthenticationFlowProvider>>()));

            // The steps this domain ships. Scoped because each reads through providers that are,
            // and registered by name here so a flow row naming one resolves to it.
            builder.Services.TryAddScoped<PasswordCredentialStep>();
            builder.Services.TryAddScoped<BakePermissionsStep>();
            builder.Services.TryAddScoped<AuthorizeIssuanceStep>();
            builder.Services.TryAddScoped<ResolvePrincipalStep>();

            builder.Services.TryAddSingleton<IPasswordCredentialAccessor, HttpPasswordCredentialAccessor>();
            builder.Services.AddHttpContextAccessor();

            builder.Services.TryAddScoped(sp => new AuthenticationRunner(
                sp.GetRequiredService<IAuthenticationStepResolver>(),
                sp.GetRequiredService<IAcrPolicy>(),
                sp.GetRequiredService<ITokenIssuer>(),
                sp.GetRequiredService<IAuthenticationExecutionStore>(),
                TimeSpan.FromMinutes(10),
                sp.GetService<ILogger<AuthenticationRunner>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Initialize and not Register: a step is resolved per request but named once, and the
        // resolver holding the names is a singleton. Registering the names here - after the
        // container exists - is what lets a scoped step be reached from it without the resolver
        // capturing a scope it would outlive.
        Initialization((host, loggerFactory) =>
        {
            // A scope, because the resolver is scoped. Registration only writes the static
            // name-to-type map, so this scope exists to construct the resolver and nothing else.
            using var scope = host.Services.CreateScope();
            var resolver = scope.ServiceProvider.GetRequiredService<AuthenticationStepResolver>();

            // Named for what the step does, not for its class. A flow row names the behaviour it
            // wants; which type provides it is this registration's business.
            var registered = resolver.Register("PasswordCredential", typeof(PasswordCredentialStep));

            if (registered.IsFailure)
                return registered.ToNewResult<IHost>();

            registered = resolver.Register("BakePermissions", typeof(BakePermissionsStep));

            if (registered.IsFailure)
                return registered.ToNewResult<IHost>();

            registered = resolver.Register("AuthorizeIssuance", typeof(AuthorizeIssuanceStep));

            if (registered.IsFailure)
                return registered.ToNewResult<IHost>();

            registered = resolver.Register("ResolvePrincipal", typeof(ResolvePrincipalStep));

            return registered.IsFailure
                ? registered.ToNewResult<IHost>()
                : GenericResult<IHost>.Success(host);
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
