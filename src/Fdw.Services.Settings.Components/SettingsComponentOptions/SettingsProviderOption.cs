using Fdw.Collections.Attributes;
using Fdw.Services.Settings.Components.Settings;

namespace Fdw.Services.Settings.Components.SettingsComponentOptions;

/// <summary>The settings component.</summary>
/// <remarks>
/// Declares no Registration body, and that is the correct shape rather than an omission. The named
/// HttpClients this provider resolves — SettingsClient and ThemeClient — are registered by their
/// own client service types (SettingsClientType, ThemeClientType) against ApiClientTypes, which
/// resolve each address from ApiClients:{Name}:BaseUrl. Registering them here as well would add a
/// second client under the same name with no BaseAddress and shadow the one that works.
///
/// A component's Registration body is for what only THIS component needs and nothing else
/// registers. Most components need nothing: they resolve typed clients that already have owners.
/// </remarks>
[TypeOption(typeof(SettingsComponents), "Settings")]
public class SettingsProviderOption : SettingsComponentBase<SettingsProvider>
{
}
