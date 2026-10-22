using Fdw.Services.Authentication.Flow;
using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authentication.Binding;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.ServiceTypes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Execution;
using Fdw.Services.Authentication.Execution;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Security.Encryption;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Fdw.Services.Authorization;
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

            builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
            // The levels are the deployment's, declared by the packages the host references; read when
            // the policy is first asked for, after every referenced option has joined the collection.
            builder.Services.TryAddSingleton<IAcrPolicy>(sp => new StandardAcrPolicy(
                AssuranceLevels.All(), sp.GetService<ILogger<StandardAcrPolicy>>()));

            // What the shipped steps take beyond their own registration. A step is activated on
            // demand, so a dependency missing here fails at the first login that names the step,
            // never at startup - which is why these were found one deploy at a time.
            builder.Services.TryAddScoped<ITenantResolver, UserTenantResolver>();
            builder.Services.TryAddScoped<IIssuanceEligibility, UserAccountEligibility>();

            // Scoped with the tenant resolver it reads through. The step lookup is the collection, the
            // same registry the runner and the flow provider are handed.
            builder.Services.TryAddScoped<ISignInMethodAdmission>(sp => new SignInMethodAdmission(
                sp.GetRequiredService<IAuthenticationFlowProvider>(),
                sp.GetRequiredService<ITenantResolver>(),
                sp.GetRequiredService<IUserPermissions>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                name => AuthenticationStepTypes.ByName(name) as IAuthenticationStep,
                sp.GetService<ILogger<SignInMethodAdmission>>()));

            // What an administration console changes flows through. Scoped for the same reason: it
            // resolves the flow a change is made to through the collection, and reloads the singleton
            // flow provider so the change is enforced on this host without a restart (FDW-912).
            builder.Services.TryAddScoped<IAuthenticationFlowAdministration>(sp => new AuthenticationFlowAdministration(
                sp.GetRequiredService<IAuthenticationFlowConfigurationProvider>(),
                sp.GetRequiredService<IAuthenticationFlowProvider>(),
                sp.GetRequiredService<IPermissionConfigurationProvider>(),
                sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                sp.GetRequiredService<ITenantResolver>(),
                sp.GetRequiredService<IUserPermissions>(),
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                name => AuthenticationStepTypes.ByName(name) as IAuthenticationStep,
                () => AuthenticationStepTypes.All().Values.OfType<IAuthenticationStep>(),
                sp.GetService<ILogger<AuthenticationFlowAdministration>>()));

            // Why a cache lifetime rather than a key held for the process lifetime: a secret manager is not
            // always a cheap local read (Azure Key Vault is a network round trip), so the key is reused
            // between calls, but it expires so a rotation takes effect without a restart.
            builder.Services.TryAddSingleton<IEncryptionCommandExecutor>(sp =>
                new AesGcmEncryptionCommandExecutor(
                    sp.GetRequiredService<ISecretManagerProvider>(),
                    EncryptionKeyCacheLifetime,
                    sp.GetService<ILogger<AesGcmEncryptionCommandExecutor>>()));

            builder.Services.TryAddSingleton<IAuthenticationExecutionStore, DatabaseExecutionStore>();
            builder.Services.TryAddSingleton<IAuthorizationCodeStore, DatabaseAuthorizationCodeStore>();
            // How far a session's stored idle expiry may fall behind its last request before it is written
            // again (FDW-983): stated here, where the store is registered. The same value is stated where the
            // Session authentication type registers it; TryAdd keeps whichever body runs first.
            builder.Services.TryAddSingleton<ISessionStore>(sp =>
                ActivatorUtilities.CreateInstance<DatabaseSessionStore>(sp, TimeSpan.FromMinutes(1)));
            // A session a flow delivers is read back through this by whatever holds it -- the Session
            // scheme for a cookie, a SQL Server connection for each batch. Scoped because the revocation
            // deny-list it consults is; the Session authentication type states the same registration.
            builder.Services.TryAddScoped<ISessionAuthenticator, SessionAuthenticator>();
            builder.Services.TryAddSingleton<ISessionCookieDelivery, SessionCookieDelivery>();
            builder.Services.TryAddScoped<ISessionActing, SessionActing>();

            builder.Services.TryAddSingleton<IAuthorizationRequestStore>(sp =>
                new InMemoryAuthorizationRequestStore(
                    sp.GetService<ILogger<InMemoryAuthorizationRequestStore>>()));

            // The two providers AuthenticationFlowProvider reads through. Consumed via
            // GetRequiredService and registered nowhere, so the flow provider could not be built -
            // it surfaced as a "No service for type" failure for its configuration provider
            // only once everything ahead of it in startup had been fixed.
            builder.Services.AddSingleton<IAuthenticationFlowImplementationConfigurationProvider, AuthenticationFlowImplementationConfigurationProvider>(sp => new AuthenticationFlowImplementationConfigurationProvider(sp.GetRequiredService<ILogger<AuthenticationFlowImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationStepTypes.ConfigurationConnection, "auth", "AuthenticationFlowConfiguration"));
            builder.Services.TryAddSingleton<AuthenticationFlowConfigurationProvider>(sp =>
            {
                var domain = new AuthenticationFlowConfigurationProvider(
                    sp.GetRequiredService<ILogger<AuthenticationFlowConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationStepTypes.ConfigurationConnection, "auth", "AuthenticationFlow");
                domain.Register("AuthenticationFlow", sp.GetRequiredService<IAuthenticationFlowImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IAuthenticationFlowConfigurationProvider>(sp => sp.GetRequiredService<AuthenticationFlowConfigurationProvider>());

            // What the foreign-token exchange needs. A caller arriving on another authority's token
            // is bound to a local user through ExternalIdentity rows, so the binding reads its own
            // container the way every other implementation provider does.
            builder.Services.AddSingleton<IExternalIdentityImplementationConfigurationProvider, ExternalIdentityImplementationConfigurationProvider>(sp => new ExternalIdentityImplementationConfigurationProvider(sp.GetRequiredService<ILogger<ExternalIdentityImplementationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationStepTypes.ConfigurationConnection, "auth", "ExternalIdentityConfiguration"));
            builder.Services.TryAddSingleton<ExternalIdentityConfigurationProvider>(sp =>
            {
                var domain = new ExternalIdentityConfigurationProvider(
                    sp.GetRequiredService<ILogger<ExternalIdentityConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), AuthenticationStepTypes.ConfigurationConnection, "auth", "ExternalIdentity");
                domain.Register("ExternalIdentity", sp.GetRequiredService<IExternalIdentityImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IExternalIdentityConfigurationProvider>(sp => sp.GetRequiredService<ExternalIdentityConfigurationProvider>());

            // Whoever writes an external-identity binding has to be able to ask which issuers this
            // deployment can resolve; the collection above is what knows.
            builder.Services.TryAddSingleton<IExternalIssuerCatalog, ExternalIssuerCatalog>();

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
                    SigningKeyLifetime,
                    SigningKeyMinimumRefreshInterval,
                    sp.GetService<ILogger<CachingSigningKeyProvider>>()));

            builder.Services.TryAddSingleton<IAuthenticationFlowProvider>(sp =>
                new AuthenticationFlowProvider(
                    sp.GetRequiredService<IAuthenticationFlowConfigurationProvider>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    name => AuthenticationStepTypes.ByName(name) as IAuthenticationStep,
                    sp.GetService<ILogger<AuthenticationFlowProvider>>()));

            // IPasswordCredentialAccessor and ITenantSwitchRequestAccessor are no longer registered
            // here: each moved to its own step's Registration phase in reference-servicetypes
            // (ReferenceAuthentication.PasswordCredential.Registration,
            // ReferenceAuthentication.TenantSwitch.Registration), alongside the accessor
            // implementation itself (FDW-848) -- a host that does not reference the step no longer
            // pulls in what only that step needs.
            builder.Services.AddHttpContextAccessor();

            builder.Services.TryAddScoped(sp => new AuthenticationRunner(
                sp.GetRequiredService<IAcrPolicy>(),
                sp.GetRequiredService<ITokenIssuer>(),
                sp.GetRequiredService<ISessionStore>(),
                sp.GetRequiredService<IAuthenticationExecutionStore>(),
                sp.GetRequiredService<IAuthenticationFlowProvider>(),
                // The collection IS the registry: a name resolves to the option that declared it.
                name => AuthenticationStepTypes.ByName(name) as IAuthenticationStep,
                sp.GetRequiredService<ITenantProvider>(),
                sp.GetService<ILogger<AuthenticationRunner>>()));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    /// <remarks>
    /// PlatformConfiguration, because that is where a flow's DDL actually lives:
    /// <c>auth.AuthenticationFlow</c> and <c>auth.AuthenticationFlowStepConfiguration</c> are declared in
    /// ConfigurationDb and seeded there. This previously said ServerConfiguration, on the argument
    /// that which providers a host accepts is the host's own business — a fair argument, and it
    /// contradicted the data model, so the flow provider read a store that held no flows.
    /// <para>
    /// Moving flows to ServerConfiguration would be a coherent design, but it is a schema change
    /// with a data migration behind it, not a connection-name edit. The trusted-issuer list is the
    /// part that genuinely is per-host, and that one does read from ServerConfiguration.
    /// </para>
    /// </remarks>
    public static string ConfigurationConnection => PlatformConnection.Name;

    /// <summary>
    /// The connection the authentication runtime state is written to and read from: sessions,
    /// authorization codes and in-flight step executions. It is a connection the host declares in its
    /// <c>configurationSchema.json</c>, reached through its configuration gateway and never named by a
    /// ConfigurationDb row. The name is the host's <c>ConnectionRoles.Auth</c> declaration.
    /// </summary>
    public static string StateConnection => AuthConnection.Name;

    /// <summary>
    /// How long an authority's signing keys are held before a scheduled refresh. A deployment sets
    /// this before the host registers services.
    /// </summary>
    public static TimeSpan SigningKeyLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The shortest time between two forced refreshes of an authority's signing keys, so a token
    /// naming an unknown key cannot make every request re-fetch the key document.
    /// </summary>
    public static TimeSpan SigningKeyMinimumRefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long the key that encrypts authentication state is reused before it is read from its
    /// secret manager again, so a rotation takes effect without a restart.
    /// </summary>
    public static TimeSpan EncryptionKeyCacheLifetime { get; set; } = TimeSpan.FromMinutes(10);
}
