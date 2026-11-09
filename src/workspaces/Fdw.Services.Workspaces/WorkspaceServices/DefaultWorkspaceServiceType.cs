using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Registers the workspace configuration providers, the workspace permissions and the maker of a default workspace (FDW-4556).
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(WorkspaceServiceTypes), "Default")]
public sealed class DefaultWorkspaceServiceType : WorkspaceServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DefaultWorkspaceServiceType"/> class.</summary>
    public DefaultWorkspaceServiceType()
        : base(
            "Default",
            "Workspaces:Default",
            "Default Workspace Services",
            "The workspace providers, the workspace permissions and the maker of a tenant's or organization's default workspace")
    {
        Registration((builder, loggerFactory) =>
        {
            var logger = loggerFactory?.CreateLogger<DefaultWorkspaceServiceType>() ?? NullLogger<DefaultWorkspaceServiceType>.Instance;

            builder.Services.TryAddSingleton<IWorkspaceImplementationConfigurationProvider>(sp => new WorkspaceImplementationConfigurationProvider(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<WorkspaceImplementationConfigurationProvider>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                WorkspaceServiceTypes.ConfigurationConnection, "workspace", "WorkspaceConfiguration"));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceImplementationConfigurationProvider));

            builder.Services.TryAddSingleton<WorkspaceConfigurationProvider>(sp =>
            {
                var domain = new WorkspaceConfigurationProvider(
                    sp.GetRequiredService<ILogger<WorkspaceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    WorkspaceServiceTypes.ConfigurationConnection, "workspace", "Workspace");
                domain.Register("Workspace", sp.GetRequiredService<IWorkspaceImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IWorkspaceConfigurationProvider>(sp => sp.GetRequiredService<WorkspaceConfigurationProvider>());
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspaceConfigurationProvider));

            // Scoped: the providers are singletons, the work is per request (FDW-937).
            builder.Services.TryAddScoped<IWorkspacePermissions>(sp =>
                new WorkspacePermissions(
                    sp.GetRequiredService<IPermissionConfigurationProvider>(),
                    sp.GetRequiredService<IRolePermissionConfigurationProvider>(),
                    sp.GetService<ILogger<WorkspacePermissions>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IWorkspacePermissions));

            // AddScoped, not TryAdd: Fdw.Services.Users registers a refusing IDefaultWorkspaceFounding for a host without this package (FDW-4178), and the last
            // registration wins, so this one replaces it whichever of the two registers first. Scoped like the permissions it uses; it reads the ambient
            // authentication context, because a new tenant's workspace is written as that tenant.
            builder.Services.AddScoped<IDefaultWorkspaceFounding>(sp =>
                new DefaultWorkspaceFounding(
                    sp.GetRequiredService<IWorkspaceConfigurationProvider>(),
                    sp.GetRequiredService<IWorkspacePermissions>(),
                    sp.GetRequiredService<IAuthenticationContextAccessor>(),
                    sp.GetService<ILogger<DefaultWorkspaceFounding>>()));
            WorkspaceRegistrationLog.Registered(logger, nameof(IDefaultWorkspaceFounding));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
