using System;

namespace Fdw.Conventions;

/// <summary>
/// Names the instance setting that holds a configuration value's default: a create request that leaves the value out takes
/// the setting, read for the caller's tenant (FDW-3580; any configuration type since FDW-3823, filled by
/// <c>Fdw.Services.Settings.ConfigurationCreateDefaults</c>).
/// </summary>
/// <remarks>
/// Why a setting and not an initializer: Mike, "make it configurable. I don't care what you set the default to as long
/// as it is in configuration and not hardcoded". The property carries no value of its own; the create path reads this
/// attribute and fills what the request did not state, or refuses the request when the setting is not set either.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DefaultSettingAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DefaultSettingAttribute"/> class.</summary>
    /// <param name="setting">The name of the setting that holds the default (a <c>DefaultValueSettings</c> name).</param>
    public DefaultSettingAttribute(string setting)
    {
        Setting = setting ?? throw new ArgumentNullException(nameof(setting));
    }

    /// <summary>Gets the name of the setting that holds the default.</summary>
    public string Setting { get; }
}
