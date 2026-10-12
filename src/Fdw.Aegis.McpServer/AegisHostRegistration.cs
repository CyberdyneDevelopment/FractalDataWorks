using System;
using Fdw.Aegis;
using Fdw.Aegis.Abstractions;
using Fdw.Aegis.Configuration;
using Fdw.Aegis.Targets;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Aegis.Logging;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Http;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Aegis.McpServer;

/// <summary>
/// Shared DI wiring for the Aegis Gateway MCP host. The entry point lives in reference-servicetypes and
/// drives these phases; the non-exposure test suite composes the identical, real registration graph against an
/// in-memory <see cref="ConfigurationSchema"/> (pointed at a test stub) instead of duplicating it.
/// </summary>
public static class AegisHostRegistration
{
    /// <summary>
    /// Phase 1a (before Build): binds the one ServiceTypeCollection this ConfigurationDb-free host
    /// drives — <see cref="SecretManagerTypes"/>, which registers the
    /// <c>IDomainServiceProvider&lt;ISecretManager, SecretManagerConfiguration&gt;</c> the
    /// <see cref="AegisInjector"/> resolves secret managers through.
    /// </summary>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Configure));

        var result = SecretManagerTypes.Configure(builder, loggerFactory);
        if (result.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Configure), result.CurrentMessage);
            return result;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Configure));
        return result;
    }

    // The connection the AegisCommand domain's rows are read from: the host's declared platform connection.
    private static string AegisConfigurationConnection => PlatformConnection.Name;

    private static ILogger Logger(ILoggerFactory? loggerFactory) =>
        loggerFactory?.CreateLogger(typeof(AegisHostRegistration))
        ?? NullLogger.Instance;

    /// <summary>
    /// Phase 1b (before Build): registers <see cref="SecretManagerTypes"/>' required services, one
    /// named <see cref="System.Net.Http.HttpClient"/> per declared HTTP connection, the AegisCommand
    /// domain with its PreApproved and AdHoc providers, and the Aegis injector pipeline itself.
    /// </summary>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ConfigurationSchema schema, ILoggerFactory? loggerFactory = null)
    {

        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Register));

        var secretManagersRegistered = SecretManagerTypes.Register(builder, loggerFactory);
        if (secretManagersRegistered.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Register), secretManagersRegistered.CurrentMessage);
            return secretManagersRegistered;
        }

        builder.Services.TryAddSingleton(sp => new Lazy<IConfigurationGateway>(() => sp.GetRequiredService<IConfigurationGateway>()));

        foreach (var connection in schema.Connections)
        {
            if (connection is HttpConnectionConfigurationBase http)
            {
                var baseUrl = http.BaseUrl;
                builder.Services.AddHttpClient(connection.Name, client => client.BaseAddress = new Uri(baseUrl));
            }
        }

        // The declared commands are a domain: AegisCommand rows, each naming the implementation --
        // PreApproved or AdHoc -- whose row carries the command. All three read the connection
        // the host declared as its platform connection.
        builder.Services.TryAddSingleton(sp => new PreApprovedCommandConfigurationProvider(
            sp.GetRequiredService<ILogger<PreApprovedCommandConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(),
            AegisConfigurationConnection, "aegis", "PreApprovedCommandConfiguration"));
        builder.Services.TryAddSingleton(sp => new AdHocCommandConfigurationProvider(
            sp.GetRequiredService<ILogger<AdHocCommandConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(),
            AegisConfigurationConnection, "aegis", "AdHocCommandConfiguration"));
        builder.Services.TryAddSingleton<IAegisCommandConfigurationProvider>(sp =>
        {
            var domain = new AegisCommandConfigurationProvider(
                sp.GetRequiredService<ILogger<AegisCommandConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                AegisConfigurationConnection, "aegis", "AegisCommand");
            domain.Register(ApprovalPolicyTypes.PreApproved.Name, sp.GetRequiredService<PreApprovedCommandConfigurationProvider>());
            domain.Register(ApprovalPolicyTypes.AdHoc.Name, sp.GetRequiredService<AdHocCommandConfigurationProvider>());
            return domain;
        });


        builder.Services.AddScoped<IApprovalPolicyEvaluator, PreApprovedPolicyEvaluator>();
        builder.Services.AddScoped<IAegisInjectionTarget, HttpHeaderInjectionTarget>();
        builder.Services.AddScoped<AegisInjector>();

        AegisLog.HostPhaseCompleted(logger, nameof(Register));
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Phase 2 (after Build): wires <see cref="SecretManagerTypes"/> factories into its provider, so
    /// <c>ISecretManagerProvider.Get(name)</c> resolves a logical name with zero ConfigurationDb access.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why name resolution is not an Aegis-side directory: turning a name into a configuration is the
    /// domain provider's job, not the injector's, so <c>AegisInjector</c> holds no directory and names no
    /// specific secret manager.
    /// </para>
    /// <para>
    /// The <see cref="ISecretManagerConfigurationProvider"/> that <see cref="SecretManagerTypes"/> registers
    /// reads through <see cref="IConfigurationGatewayProvider"/>, as do the three AegisCommand providers. The
    /// host supplies that gateway by running <see cref="RegisterConfigurationSource"/> first: the rows come
    /// from a declared connection (FDW-1119), not from a source Aegis owns.
    /// </para>
    /// </remarks>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(Initialize));

        var secretManagersInitialized = SecretManagerTypes.Initialize(host, loggerFactory);
        if (secretManagersInitialized.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(Initialize), secretManagersInitialized.CurrentMessage);
            return secretManagersInitialized;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(Initialize));
        return GenericResult<IHost>.Success(host);
    }

    /// <summary>
    /// Phase 1c (before Build): registers the connection domain and the configuration gateway over the schema file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The configuration source is a connection: the gateway builds from the schema's declared connection through its
    /// connection type's own factory, so nothing here knows what kind of store holds the rows. The caller names the
    /// schema file, and the schema declares which connection plays each role (<c>ConnectionRoles</c>): this host
    /// declares one connection as both Platform and Server, so every configuration domain reads its rows from it.
    /// There is no default for either, and a schema that declares no roles fails the gateway's registration.
    /// </para>
    /// <para>
    /// Why the connection domain is registered whole: it is what puts each connection type's factory in the container,
    /// and the gateway builds from that factory. Its health monitor finds no connection container in a store that
    /// declares none and idles, which is the state this host is in. The monitor opens a scope under a system
    /// authentication context, so the accessor it resolves is registered here too.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="schemaFileName">The schema file, resolved against the application base directory.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>The builder, or the failure of the phase that stopped.</returns>
    public static IGenericResult<IHostApplicationBuilder> RegisterConfigurationSource(
        IHostApplicationBuilder builder,
        string schemaFileName,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(RegisterConfigurationSource));

        ConfigurationGatewayTypes.SchemaFileName = schemaFileName;

        builder.Services.TryAddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();

        var connectionsConfigured = ConnectionTypes.Configure(builder, loggerFactory);
        var connectionsRegistered = connectionsConfigured.IsFailure
            ? connectionsConfigured
            : ConnectionTypes.Register(builder, loggerFactory);
        var gatewayConfigured = connectionsRegistered.IsFailure
            ? connectionsRegistered
            : ConfigurationGatewayTypes.Configure(builder, loggerFactory);
        var gatewayRegistered = gatewayConfigured.IsFailure
            ? gatewayConfigured
            : ConfigurationGatewayTypes.Register(builder, loggerFactory);
        if (gatewayRegistered.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(RegisterConfigurationSource), gatewayRegistered.CurrentMessage);
            return gatewayRegistered;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(RegisterConfigurationSource));
        return gatewayRegistered;
    }

    /// <summary>
    /// Phase 2a (after Build): wires each registered connection type's provider into the connection domain, so a
    /// declared connection's factory and configuration provider are reachable.
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <returns>The host, or the failure of the phase that stopped.</returns>
    public static IGenericResult<IHost> InitializeConfigurationSource(IHost host, ILoggerFactory? loggerFactory = null)
    {
        var logger = Logger(loggerFactory);
        AegisLog.HostPhaseStarting(logger, nameof(InitializeConfigurationSource));

        var connectionsInitialized = ConnectionTypes.Initialize(host, loggerFactory);
        var gatewayInitialized = connectionsInitialized.IsFailure
            ? connectionsInitialized
            : ConfigurationGatewayTypes.Initialize(host, loggerFactory);
        if (gatewayInitialized.IsFailure)
        {
            AegisLog.HostPhaseFailed(logger, nameof(InitializeConfigurationSource), gatewayInitialized.CurrentMessage);
            return gatewayInitialized;
        }

        AegisLog.HostPhaseCompleted(logger, nameof(InitializeConfigurationSource));
        return gatewayInitialized;
    }
}
