using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Settings;

/// <summary>
/// The settings that inherit down the platform, tenant and organization tree and can be locked from above (FDW-1579).
/// </summary>
/// <remarks>
/// The platform defines each setting and holds its default. A tenant, and an organization of a tenant, may set their own value unless a
/// level above locked the setting. The value in force is the nearest one set; a locked level's value governs every level below it, and
/// values set below a lock are ignored (not deleted) while it stands. Who may act at a level is settled by the caller; the organization
/// scope is checked here, because an organization is only reached by someone acting in it.
/// </remarks>
public interface IInheritedSettings
{
    /// <summary>Lists every setting the platform defines, as it stands at a level.</summary>
    /// <param name="scope">The level.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The settings, each with its value, where it was set and whether it is locked.</returns>
    Task<IGenericResult<IReadOnlyList<EffectiveSetting>>> List(SettingScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads one setting as it stands at a level.</summary>
    /// <param name="scope">The level.</param>
    /// <param name="settingName">The setting.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The setting, or a not-found failure when the platform defines none of that name.</returns>
    Task<IGenericResult<EffectiveSetting>> Get(SettingScope scope, string settingName, CancellationToken cancellationToken = default);

    /// <summary>Sets a setting's value at a level, and locks it against the levels below or leaves it open to them.</summary>
    /// <param name="scope">The level the value is set at.</param>
    /// <param name="settingName">The setting.</param>
    /// <param name="value">The value.</param>
    /// <param name="locked">True to bar the levels below from setting their own value. An organization has no level below it, so it cannot lock.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The setting as it now stands at that level, or a conflict failure when a level above locked it.</returns>
    Task<IGenericResult<EffectiveSetting>> Set(SettingScope scope, string settingName, string value, bool locked, CancellationToken cancellationToken = default);

    /// <summary>Lifts the lock a level holds on a setting, leaving its value.</summary>
    /// <param name="scope">The level that holds the lock.</param>
    /// <param name="settingName">The setting.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The setting as it now stands at that level, or a conflict failure when that level holds no lock.</returns>
    Task<IGenericResult<EffectiveSetting>> Lift(SettingScope scope, string settingName, CancellationToken cancellationToken = default);
}
