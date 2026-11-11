using System;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// Reads the setting that says whether founding makes a default workspace (FDW-4599): the settings row, then the value <see cref="WorkspaceFoundingOption"/> declares or a host
/// overrides, then a coded failure.
/// </summary>
public sealed class WorkspaceFoundingPolicy : IWorkspaceFoundingPolicy
{
    private const string SourceSetting = "the settings row";
    private const string SourceDeclared = "the declared default";

    private readonly IEffectiveSettingsProvider _settings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceFoundingPolicy"/> class.</summary>
    /// <param name="settings">The settings provider.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceFoundingPolicy(IEffectiveSettingsProvider settings, ILogger<WorkspaceFoundingPolicy>? logger = null)
    {
        _settings = settings;
        _logger = logger ?? NullLogger<WorkspaceFoundingPolicy>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult<bool> FoundingMakesDefaultWorkspace()
    {
        if (_settings.TryGetEffectiveValue<bool>(WorkspaceFoundingOption.FoundingMakesDefaultWorkspace, out var stated))
        {
            WorkspaceFoundingLog.Resolved(_logger, stated, SourceSetting);
            return GenericResult<bool>.Success(stated);
        }

        if (ServiceTypeDefaults.TryResolve(WorkspaceFoundingOption.Instance, WorkspaceFoundingOption.FoundingMakesDefaultWorkspace, out var declared) && declared is bool answer)
        {
            WorkspaceFoundingLog.Resolved(_logger, answer, SourceDeclared);
            return GenericResult<bool>.Success(answer);
        }

        return GenericResult<bool>.Failure(WorkspaceFoundingLog.SettingNotDeclared(_logger));
    }
}
