using System;
using Fdw.Configuration;

namespace Fdw.Services.Settings.Configuration;

/// <summary>The contract every ServerSetting implementation carries.</summary>
public interface IServerSettingImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid ServerSettingId { get; set; }

    /// <summary>Gets or sets the value the setting holds, as text.</summary>
    string SettingValue { get; set; }

    /// <summary>Gets or sets the data type the value is read as.</summary>
    string DataType { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the lower bound a numeric value is clamped to.</summary>
    string? MinValue { get; set; }

    /// <summary>Gets or sets the upper bound a numeric value is clamped to.</summary>
    string? MaxValue { get; set; }

    /// <summary>Gets or sets whether the setting is in force.</summary>
    bool IsActive { get; set; }
}
