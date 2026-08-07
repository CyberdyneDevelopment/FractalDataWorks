using System;
using Fdw.Services.Results;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Workspace.Roslyn;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Connection type definition for RoslynWorkspace connections.
/// Registers <see cref="IRoslynWorkspaceConnectionFactory"/> and binds
/// <see cref="RoslynWorkspaceConnectionConfiguration"/> from <c>Connections:RoslynWorkspace</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(ConnectionTypes), "RoslynWorkspace")]
public sealed class RoslynWorkspaceConnectionType
    : ConnectionTypeBase<IGenericConnection, IRoslynWorkspaceConnectionFactory, RoslynWorkspaceConnectionConfiguration>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspaceConnectionType"/> class.
    /// </summary>
    public RoslynWorkspaceConnectionType() : base(
        name: "RoslynWorkspace",
        sectionName: "RoslynWorkspace",
        displayName: "Roslyn Workspace",
        description: "Roslyn MSBuildWorkspace for source code analysis and navigation",
        category: "Development")
    {
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = (DefaultConnectionProvider)services.GetRequiredService<IConnectionProvider>();

            // Why: attach the typed-body provider to the HEADER provider so conn.RoslynWorkspaceConnection is
            // reachable by discriminator dispatch, exactly as every other connection type does. This was the
            // only ConnectionTypes option missing the call, and its absence was not inert: a live
            // conn.Connection row with ServiceOptionType 'RoslynWorkspace' could not be read (ComposeTypedBody
            // hit OnNoTypedProvider) and its body row could never be retired.
            services.GetRequiredService<ConnectionConfigurationProvider>()
                .Register(
                    Name, services.GetRequiredService<RoslynWorkspaceConnectionConfigurationProvider>());
    
            return host;
        });

        Configuration(builder =>
        {

    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.AddSingleton<IRoslynWorkspaceFactory, RoslynWorkspaceFactory>();
            // Why: a Roslyn workspace is opened from a solution path on disk and declares no authentication
            // type, so this is the ONE connection kind that legitimately gets the no-secret-manager
            // constructor. DI picks it because there is no other.
            builder.Services.AddSingleton<IRoslynWorkspaceConnectionFactory, RoslynWorkspaceConnectionFactory>();
            builder.Services.TryAddSingleton<RoslynWorkspaceConnectionConfigurationProvider>(sp =>
                new RoslynWorkspaceConnectionConfigurationProvider(
                    sp.GetService<ILogger<RoslynWorkspaceConnectionConfigurationProvider>>()
                        ?? NullLogger<RoslynWorkspaceConnectionConfigurationProvider>.Instance,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    dataStoreName,
                    pathName,
                    new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<RoslynWorkspaceConnectionConfiguration>>(
                sp => sp.GetRequiredService<RoslynWorkspaceConnectionConfigurationProvider>());
            // Why: RegisterFactory (below) requires ConnectionConfigurationProvider (the shared header
            // provider for the whole Connections domain) to already be registered. Every other connection
            // kind calls this; RoslynWorkspace did not, so in a host where it was the only registered
            // connection kind the header provider was never registered at all.
            ConnectionConfigurationProvider.RegisterDomainConfiguration(builder.Services);
            return builder;
        });

    }

    /// <summary>Phase 1: Register factory with main DI container.</summary>

    /// <summary>Phase 2: Resolve factory from DI and register with connection provider.</summary>

    /// <summary>Phase 3: Bind RoslynWorkspaceConnectionConfiguration from appsettings.</summary>

    /// <summary>
    /// RoslynWorkspace supports WorkspaceGraph and GetSymbolSource capabilities.
    /// </summary>
    // Why: ByName() returns the TypeCollection singleton — never instantiate capabilities inline.
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("WorkspaceGraph"),
        CommandCapabilityTypes.ByName("GetSymbolSource"),
    ];

}
