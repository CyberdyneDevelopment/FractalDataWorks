using System.Collections.Generic;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The settings as they stand at a level of the tenant and organization tree (FDW-1579).
/// </summary>
public class InheritedSettingsResponse
{
    /// <summary>Gets or sets the settings.</summary>
    public IList<InheritedSettingResponse> Settings { get; set; } = [];
}
