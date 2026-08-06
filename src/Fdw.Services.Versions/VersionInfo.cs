using System;

namespace Fdw.Services.Versions;

/// <summary>
/// Contains version information for a specific assembly or group of assemblies.
/// </summary>
public sealed class VersionInfo
{
    /// <summary>
    /// Gets or sets the name of the assembly or group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version string.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this represents a group of assemblies.
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// Gets or sets the count of assemblies in this group.
    /// </summary>
    public int AssemblyCount { get; set; }
}
