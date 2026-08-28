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

namespace Fdw.Services.Connections.RoslynWorkspace.Registration;

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
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            services.GetRequiredService<ConnectionConfigurationProvider>()
                .Register(
                    Name, services.GetRequiredService<RoslynWorkspaceConnectionConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.AddSingleton<IRoslynWorkspaceFactory, RoslynWorkspaceFactory>();
            builder.Services.AddSingleton<IRoslynWorkspaceConnectionFactory, RoslynWorkspaceConnectionFactory>();
            builder.Services.TryAddSingleton<RoslynWorkspaceConnectionConfigurationProvider>(sp =>
                new RoslynWorkspaceConnectionConfigurationProvider(
                    sp.GetService<ILogger<RoslynWorkspaceConnectionConfigurationProvider>>()
                        ?? NullLogger<RoslynWorkspaceConnectionConfigurationProvider>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    DataStore,
                    PathName));
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<RoslynWorkspaceConnectionConfiguration>>(
                sp => sp.GetRequiredService<RoslynWorkspaceConnectionConfigurationProvider>());
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <summary>Phase 1: Register factory with main DI container.</summary>

    /// <summary>Phase 2: Resolve factory from DI and register with connection provider.</summary>

    /// <summary>Phase 3: Bind RoslynWorkspaceConnectionConfiguration from appsettings.</summary>

    /// <summary>
    /// RoslynWorkspace supports WorkspaceGraph and GetSymbolSource capabilities.
    /// </summary>
    public override IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
    [
        CommandCapabilityTypes.ByName("WorkspaceGraph"),
        CommandCapabilityTypes.ByName("GetSymbolSource"),
    ];

}
