using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The value a server setting holds, its data type and its bounds.</summary>
/// <remarks>
/// The implementation half of a server setting. Its domain row, <c>settings.ServerSetting</c>,
/// carries the name; this row carries everything the setting is.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Settings", ServiceType = "ServerSetting")]
public sealed partial class ServerSettingImplementationConfiguration : IServerSettingImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;


    /// <inheritdoc/>
    public string SettingValue { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string DataType { get; set; } = string.Empty;

    /// <inheritdoc/>
    // Why [NotStored]: a setting's description is a row of the descriptions table under the description type ServerSetting, written in the unit of the setting's save
    // (FDW-6208); the column is gone from settings.ServerSettingConfiguration. It is never filled on load, so it is null until a caller states one, and it has no initializer.
    [NotStored]
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? MinValue { get; set; }

    /// <inheritdoc/>
    public string? MaxValue { get; set; }

    /// <inheritdoc/>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets whether the levels below this one are barred from setting their own value (FDW-1579).</summary>
    public bool IsLocked { get; set; }
}
