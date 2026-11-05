using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>
/// FDW's rule for document definitions held at the platform, a tenant or an organization (FDW-3398): the most specific definition of a
/// code is in force, unless a level above locked the code, and a code no level defines is refused, never defaulted.
/// </summary>
/// <remarks>
/// <para>
/// The levels and the lock rule are FDW-1579's: walking down from the platform, a level's definition replaces the one above it unless a
/// level above locked the code, and the first lock found governs every level below it. A definition set below a lock is kept but not in
/// force while the lock stands. An organization cannot lock, having no level below it.
/// </para>
/// <para>
/// Unlike FDW-1579's settings, a definition need not start at the platform: a tenant or an organization defines codes of its own. That is
/// why this is not <c>IInheritedSettings</c>, which knows only the settings the platform defines.
/// </para>
/// <para>
/// Rows of another tenant or another organization are never in force, whatever the application passes in.
/// </para>
/// </remarks>
public static class ScopedDefinitions
{
    /// <summary>Finds the definition of a code in force for an organization, or for a tenant's own view.</summary>
    /// <typeparam name="T">The kind of definition.</typeparam>
    /// <param name="rows">The rows <see cref="IDocumentDefinitions{T}.Rows"/> returned.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the tenant's own view.</param>
    /// <param name="code">The code.</param>
    /// <returns>
    /// The definition in force with the level it is held at and the level that locked it, or a failure coded
    /// <c>DocumentDefinitionNotConfigured</c> when no level defines the code, or <c>DocumentDefinitionAmbiguous</c> when a level that would
    /// be in force defines it more than once.
    /// </returns>
    public static IGenericResult<EffectiveDefinition<T>> Effective<T>(IReadOnlyList<T> rows, Guid tenantId, Guid? organizationId, string code)
        where T : IScopedDefinition
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("The code must be named.", nameof(code));
        }

        EffectiveDefinition<T>? inForce = null;
        foreach (var (level, found) in Levels(rows, tenantId, organizationId, code))
        {
            if (inForce?.LockedAt is not null)
            {
                break;
            }

            if (found.Count > 1)
            {
                return GenericResult<EffectiveDefinition<T>>.Failure(
                    DocumentRecordResultCodes.ByName("DocumentDefinitionAmbiguous"),
                    ResultDetails.Create("Kind", typeof(T).Name, "Code", code, "Level", level)
                        .With("Count", found.Count)
                        .With("Scope", ScopeOf(tenantId, organizationId)));
            }

            if (found.Count == 1)
            {
                inForce = new EffectiveDefinition<T>(
                    found[0], level, found[0].Locked && !string.Equals(level, DefinitionLevels.Organization, StringComparison.Ordinal) ? level : null);
            }
        }

        return inForce is null
            ? GenericResult<EffectiveDefinition<T>>.Failure(
                DocumentRecordResultCodes.ByName("DocumentDefinitionNotConfigured"),
                ResultDetails.Create("Kind", typeof(T).Name, "Code", code, "Scope", ScopeOf(tenantId, organizationId)))
            : GenericResult<EffectiveDefinition<T>>.Success(inForce);
    }

    /// <summary>Finds every code in force for an organization, or for a tenant's own view, each resolved as <see cref="Effective{T}"/> does.</summary>
    /// <typeparam name="T">The kind of definition.</typeparam>
    /// <param name="rows">The rows <see cref="IDocumentDefinitions{T}.Rows"/> returned.</param>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the tenant's own view.</param>
    /// <returns>The definitions in force, one per code; an empty list when no level defines anything; or the first refusal.</returns>
    public static IGenericResult<IReadOnlyList<EffectiveDefinition<T>>> All<T>(IReadOnlyList<T> rows, Guid tenantId, Guid? organizationId)
        where T : IScopedDefinition
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        var inForce = new List<EffectiveDefinition<T>>();
        foreach (var code in rows.Where(row => Bears(row, tenantId, organizationId)).Select(row => row.Code).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var effective = Effective(rows, tenantId, organizationId, code);
            if (effective.IsFailure || effective.Value is null)
            {
                return effective.ToNewResult<IReadOnlyList<EffectiveDefinition<T>>>();
            }

            inForce.Add(effective.Value);
        }

        return GenericResult<IReadOnlyList<EffectiveDefinition<T>>>.Success(inForce);
    }

    /// <summary>Says whether a level may hold a definition of a code: not below a lock, and an organization never with a lock of its own.</summary>
    /// <typeparam name="T">The kind of definition.</typeparam>
    /// <param name="rows">The rows <see cref="IDocumentDefinitions{T}.Rows"/> returned for the tenant and, for an organization, the organization.</param>
    /// <param name="tenantId">The tenant that would hold it, or null for the platform.</param>
    /// <param name="organizationId">The organization that would hold it, or null for the platform or a tenant; an organization needs its tenant.</param>
    /// <param name="code">The code.</param>
    /// <param name="locked">Whether the definition would lock the code against the levels below.</param>
    /// <returns>Success when the definition may be held, or a failure coded <c>DocumentDefinitionLocked</c> naming the level and the reason.</returns>
    public static IGenericResult MayDefine<T>(IReadOnlyList<T> rows, Guid? tenantId, Guid? organizationId, string code, bool locked)
        where T : IScopedDefinition
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("The code must be named.", nameof(code));
        }

        if (organizationId is not null && tenantId is null)
        {
            throw new ArgumentException("An organization's definition needs its tenant, whose locks bear on it.", nameof(tenantId));
        }

        var level = organizationId is not null ? DefinitionLevels.Organization : tenantId is not null ? DefinitionLevels.Tenant : DefinitionLevels.Platform;
        if (locked && organizationId is not null)
        {
            return Refused<T>(code, level, "an organization has no level below it, so it cannot lock");
        }

        if (tenantId is not null && rows.Any(row => IsPlatform(row) && Same(row.Code, code) && row.Locked))
        {
            return Refused<T>(code, level, "the Platform level locked it");
        }

        return organizationId is not null && tenantId is { } tenant && rows.Any(row => IsTenant(row, tenant) && Same(row.Code, code) && row.Locked)
            ? Refused<T>(code, level, "the Tenant level locked it")
            : GenericResult.Success();
    }

    // The rows of the code at each level that bears on the scope, platform first.
    private static IEnumerable<(string Level, IReadOnlyList<T> Found)> Levels<T>(IReadOnlyList<T> rows, Guid tenantId, Guid? organizationId, string code)
        where T : IScopedDefinition
    {
        yield return (DefinitionLevels.Platform, Matching(rows, code, row => IsPlatform(row)));
        yield return (DefinitionLevels.Tenant, Matching(rows, code, row => IsTenant(row, tenantId)));
        if (organizationId is { } organization)
        {
            yield return (DefinitionLevels.Organization, Matching(rows, code, row => row.OrganizationId == organization));
        }
    }

    private static IReadOnlyList<T> Matching<T>(IReadOnlyList<T> rows, string code, Func<T, bool> atLevel)
        where T : IScopedDefinition
        => [.. rows.Where(row => atLevel(row) && Same(row.Code, code))];

    private static bool Bears<T>(T row, Guid tenantId, Guid? organizationId)
        where T : IScopedDefinition
        => IsPlatform(row) || IsTenant(row, tenantId) || (organizationId is { } organization && row.OrganizationId == organization);

    private static bool IsPlatform<T>(T row)
        where T : IScopedDefinition
        => row.TenantId is null && row.OrganizationId is null;

    private static bool IsTenant<T>(T row, Guid tenantId)
        where T : IScopedDefinition
        => row.TenantId == tenantId && row.OrganizationId is null;

    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static string ScopeOf(Guid tenantId, Guid? organizationId) =>
        organizationId is { } organization ? string.Concat("organization '", organization.ToString("D", CultureInfo.InvariantCulture), "'") : string.Concat("tenant '", tenantId.ToString("D", CultureInfo.InvariantCulture), "'");

    private static IGenericResult Refused<T>(string code, string level, string reason) =>
        GenericResult.Failure(
            DocumentRecordResultCodes.ByName("DocumentDefinitionLocked"),
            ResultDetails.Create("Kind", typeof(T).Name, "Code", code, "Level", level).With("Reason", reason));
}
