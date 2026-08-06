using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace Fdw.Services.Versions;

/// <summary>
/// Serilog enricher that adds package version information to log events.
/// Uses assembly metadata (not type activation) to summarize loaded FDW packages.
/// </summary>
public sealed class PackageVersionEnricher : ILogEventEnricher
{
    private static string? _cachedEnrichmentString;
    private static readonly object _lock = new object();

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var versions = GetVersionString();
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("PackageVersions", versions));
    }

    private static string GetVersionString()
    {
        if (_cachedEnrichmentString != null) return _cachedEnrichmentString;

        lock (_lock)
        {
            if (_cachedEnrichmentString != null) return _cachedEnrichmentString;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && (a.FullName.StartsWith("Fdw.", StringComparison.OrdinalIgnoreCase) ||
                                                 a.FullName.StartsWith("CyberdyneDevelopment.", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var groups = new List<string>();

            var fdwVersions = assemblies
                .Where(a =>
                {
                    var name = a.GetName().Name;
                    return name != null && name.StartsWith("Fdw.", StringComparison.OrdinalIgnoreCase);
                })
                .Select(a =>
                {
                    var assemblyName = a.GetName();
                    return a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                           ?? assemblyName.Version?.ToString()
                           ?? assemblyName.FullName;
                })
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Version = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            if (fdwVersions.Count > 0)
            {
                var main = fdwVersions[0];
                if (main.Count > 1)
                {
                    groups.Add($"FDW (Core) v{main.Version} ({main.Count})");
                    foreach (var outlier in fdwVersions.Skip(1))
                    {
                        groups.Add($"FDW (Outlier) v{outlier.Version} ({outlier.Count})");
                    }
                }
                else
                {
                    foreach (var v in fdwVersions.Take(3))
                    {
                        groups.Add(v.Version);
                    }
                }
            }

            _cachedEnrichmentString = string.Join(", ", groups);
            return _cachedEnrichmentString;
        }
    }
}
