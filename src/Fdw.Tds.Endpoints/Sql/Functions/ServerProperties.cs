using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// What SERVERPROPERTY answers, from the server identity this endpoint presents.
/// </summary>
/// <remarks>
/// The features a real server may install — clustering, availability groups, full-text, PolyBase — are not
/// here, so each answers 0. A property name SQL Server does not define is NULL, which is SQL Server's own
/// answer to an unknown name.
/// </remarks>
public static class ServerProperties
{
    private static readonly Dictionary<string, Func<SqlExecutionContext, object?>> Properties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductVersion"] = static c => c.Server.Version.ToString(),
        ["ResourceVersion"] = static c => c.Server.Version.ToString(),
        ["ProductMajorVersion"] = static c => c.Server.Version.Major.ToString(CultureInfo.InvariantCulture),
        ["ProductMinorVersion"] = static c => c.Server.Version.Minor.ToString(CultureInfo.InvariantCulture),
        ["ProductBuild"] = static c => c.Server.Version.Build.ToString(CultureInfo.InvariantCulture),
        ["ProductLevel"] = static _ => "RTM",
        ["ProductUpdateLevel"] = static _ => null,
        ["Edition"] = static c => c.Server.Edition,
        ["EditionID"] = static c => EditionId(c.Server.Edition),
        ["EngineEdition"] = static c => c.Server.EngineEdition,
        ["MachineName"] = static c => c.Server.ServerName,
        ["ServerName"] = static c => c.Server.ServerName,
        ["InstanceName"] = static _ => null,
        ["Collation"] = static c => c.Server.Collation.Name,
        ["LCID"] = static c => Lcid(c),
        ["ProcessID"] = static _ => Environment.ProcessId,
        ["BuildClrVersion"] = static _ => "v4.0.30319",
        ["IsClustered"] = static _ => 0,
        ["IsHadrEnabled"] = static _ => 0,
        ["IsIntegratedSecurityOnly"] = static _ => 0,
        ["IsFullTextInstalled"] = static _ => 0,
        ["IsSingleUser"] = static _ => 0,
        ["IsLocalDB"] = static _ => 0,
        ["IsXTPSupported"] = static _ => 0,
        ["IsPolyBaseInstalled"] = static _ => 0,
        ["IsAdvancedAnalyticsInstalled"] = static _ => 0,
        ["IsExternalAuthenticationOnly"] = static _ => 0,
        ["IsTempDbMetadataMemoryOptimized"] = static _ => 0,
    };

    private static readonly Dictionary<string, long> EditionIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enterprise"] = 1804890536,
        ["Standard"] = -1534726760,
        ["Express"] = -1592396055,
        ["Developer"] = -2117995310,
        ["Web"] = 1674378470,
    };

    /// <summary>Reads a property.</summary>
    /// <param name="name">The property's name.</param>
    /// <param name="context">The request.</param>
    /// <param name="value">The value; null for NULL.</param>
    /// <returns>False for a name SQL Server does not define.</returns>
    public static bool TryRead(string name, SqlExecutionContext context, out object? value)
    {
        if (Properties.TryGetValue(name, out var read))
        {
            value = read(context);
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>Gets the locale id the collation carries in its first 20 bits.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The locale id, such as 1033.</returns>
    public static int Lcid(SqlExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var bytes = context.Server.Collation.Bytes;
        return bytes[0] | (bytes[1] << 8) | ((bytes[2] & 0x0F) << 16);
    }

    // The edition id SQL Server reports for an edition, read from the edition's leading word.
    private static long? EditionId(string edition)
    {
        var word = edition.Split(' ', 2)[0];
        return EditionIds.TryGetValue(word, out var id) ? id : null;
    }
}
