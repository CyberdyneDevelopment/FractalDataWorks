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

    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid ServerSettingId { get; set; }


    /// <inheritdoc/>
    public string SettingValue { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string DataType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? MinValue { get; set; }

    /// <inheritdoc/>
    public string? MaxValue { get; set; }

    /// <inheritdoc/>
    public bool IsActive { get; set; } = true;
}
