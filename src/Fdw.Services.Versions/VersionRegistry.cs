using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Versions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Versions;

/// <summary>
/// Registry for discovering and grouping package versions in the ecosystem.
/// Uses assembly metadata (not type activation) to enumerate loaded FDW packages.
/// </summary>
public sealed class VersionRegistry
{
    private readonly ILogger<VersionRegistry> _logger;
    private readonly List<VersionInfo> _cachedVersions = new List<VersionInfo>();
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public VersionRegistry(ILogger<VersionRegistry>? logger = null)
    {
        _logger = logger ?? NullLogger<VersionRegistry>.Instance;
    }

    /// <summary>
    /// Gets all discovered package versions, using cached results if available.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of version info entries.</returns>
    public Task<IGenericResult<IReadOnlyList<VersionInfo>>> GetVersions(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cachedVersions.Count > 0)
            {
                return Task.FromResult(GenericResult<IReadOnlyList<VersionInfo>>.Success(_cachedVersions.AsReadOnly()));
            }
        }

        return RefreshVersions(ct);
    }

    /// <summary>
    /// Refreshes the version cache by re-scanning loaded assemblies.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the refreshed list of version info entries.</returns>
    public Task<IGenericResult<IReadOnlyList<VersionInfo>>> RefreshVersions(CancellationToken ct = default)
    {
        try
        {
            VersionLog.DiscoveryStarted(_logger, "Fdw.*|CyberdyneDevelopment.*");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && (a.FullName.StartsWith("Fdw.", StringComparison.OrdinalIgnoreCase) ||
                                                 a.FullName.StartsWith("CyberdyneDevelopment.", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var assemblyVersions = assemblies
                .Select(a =>
                {
                    var assemblyName = a.GetName();
                    return new AssemblyVersionEntry(
                        assemblyName.Name ?? assemblyName.FullName,
                        a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? assemblyName.Version?.ToString()
                            ?? assemblyName.FullName);
                })
                .ToList();

            var groups = new List<VersionInfo>();

            var fdwAssemblies = assemblyVersions.Where(a => a.Name.StartsWith("Fdw.", StringComparison.OrdinalIgnoreCase)).ToList();
            ProcessGrouping(fdwAssemblies, "Fdw Core", groups);

            var cdAssemblies = assemblyVersions.Where(a => a.Name.StartsWith("CyberdyneDevelopment.", StringComparison.OrdinalIgnoreCase)).ToList();
            ProcessGrouping(cdAssemblies, "Cyberdyne Ecosystem", groups);

            lock (_lock)
            {
                _cachedVersions.Clear();
                _cachedVersions.AddRange(groups);
            }

            VersionLog.DiscoveryCompleted(_logger, assemblyVersions.Count, groups.Count);
            return Task.FromResult(GenericResult<IReadOnlyList<VersionInfo>>.Success(groups.AsReadOnly()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<VersionInfo>>.Failure(VersionLog.DiscoveryFailed(_logger, ex, ex.Message)));
        }
    }

    private void ProcessGrouping(IReadOnlyList<AssemblyVersionEntry> assemblies, string groupName, List<VersionInfo> resultList)
    {
        if (assemblies.Count == 0) return;

        var versionCounts = assemblies
            .GroupBy(a => a.Version, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Version = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var dominant = versionCounts[0];

        if (dominant.Count >= assemblies.Count * 0.5)
        {
            resultList.Add(new VersionInfo
            {
                Name = groupName,
                Version = dominant.Version,
                IsGroup = true,
                AssemblyCount = dominant.Count
            });

            VersionLog.GroupDetected(_logger, groupName, dominant.Version, dominant.Count);

            foreach (var outlier in assemblies.Where(a => !string.Equals(a.Version, dominant.Version, StringComparison.OrdinalIgnoreCase)))
            {
                resultList.Add(new VersionInfo
                {
                    Name = outlier.Name,
                    Version = outlier.Version,
                    IsGroup = false,
                    AssemblyCount = 1
                });
            }
        }
        else
        {
            foreach (var assembly in assemblies)
            {
                resultList.Add(new VersionInfo
                {
                    Name = assembly.Name,
                    Version = assembly.Version,
                    IsGroup = false,
                    AssemblyCount = 1
                });
            }
        }
    }

    private sealed class AssemblyVersionEntry
    {
        public string Name { get; }
        public string Version { get; }

        public AssemblyVersionEntry(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
