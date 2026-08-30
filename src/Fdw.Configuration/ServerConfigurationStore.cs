using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Fdw.Configuration;

/// <summary>
/// Reads a table out of the <c>ServerConfiguration</c> store during the registration phase.
/// </summary>
/// <remarks>
/// <para>
/// ServerConfiguration holds what a host would otherwise put in <c>appsettings.json</c> or wire by
/// hand in <c>Program.cs</c> — which issuers it trusts, and on what terms. That is the host's own
/// business rather than the tenant's, which is why it is a separate connection from
/// PlatformConfiguration.
/// </para>
/// <para>
/// It is read here rather than through <c>IConfigurationGateway</c> because of when it is needed.
/// Registering an authentication scheme has to happen in the registration phase, before the
/// container exists, and <c>ConfigurationGatewayTypes</c> registers in that same phase — after
/// Authentication, since PlatformServices runs domains alphabetically. So a gateway read at this
/// point is not merely awkward, it is asking for a service that has not been registered yet.
/// </para>
/// <para>
/// The consequence, stated plainly: this reads a file-backed ServerConfiguration. A
/// database-backed one cannot be read this early by anything, and would need its consumers to
/// move to a later phase rather than a different reader here.
/// </para>
/// </remarks>
public static class ServerConfigurationStore
{
    /// <summary>The connection name a host declares its own server-scoped configuration under.</summary>
    public const string ConnectionName = "ServerConfiguration";

    /// <summary>
    /// Reads <paramref name="table"/> from <paramref name="path"/> and exposes its rows as
    /// <paramref name="sectionName"/>.
    /// </summary>
    /// <param name="schemaFileName">The bootstrap schema declaring the store.</param>
    /// <param name="path">The store's folder for this domain, e.g. <c>auth</c>.</param>
    /// <param name="table">The file's base name, without extension.</param>
    /// <param name="sectionName">The configuration section the rows are exposed under.</param>
    /// <returns>
    /// A configuration carrying the rows. Empty when the store, the folder or the file is absent —
    /// a host that declares no entries is a host with none, which its consumer reports in its own
    /// terms rather than being told here that a file is missing.
    /// </returns>
    public static IConfiguration Read(
        string schemaFileName, string path, string table, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("A table name is required.", nameof(table));
        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("A section name is required.", nameof(sectionName));

        return Root(schemaFileName) is { } root
            ? Build(Path.Combine(root, path ?? string.Empty, table + ".json"), sectionName)
            : Empty();
    }

    // The declared root, read straight out of the schema JSON rather than through the typed
    // connection body: that body's type lives in whichever package implements the connection, and
    // this runs in a package that does not reference it.
    private static string? Root(string schemaFileName)
    {
        if (string.IsNullOrWhiteSpace(schemaFileName))
            return null;

        var schemaPath = Path.IsPathRooted(schemaFileName)
            ? schemaFileName
            : Path.Combine(AppContext.BaseDirectory, schemaFileName);

        if (!File.Exists(schemaPath))
            return null;

        using var document = JsonDocument.Parse(File.ReadAllBytes(schemaPath));

        if (!document.RootElement.TryGetProperty("ConfigurationSchema", out var schema)
            || !schema.TryGetProperty("Connections", out var connections)
            || connections.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var connection in connections.EnumerateArray())
        {
            if (!connection.TryGetProperty("Name", out var name)
                || !string.Equals(name.GetString(), ConnectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!connection.TryGetProperty("Configuration", out var body)
                || !body.TryGetProperty("Root", out var declaredRoot)
                || declaredRoot.GetString() is not { Length: > 0 } value)
                return null;

            return Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value);
        }

        return null;
    }

    private static IConfiguration Build(string file, string sectionName)
    {
        if (!File.Exists(file))
            return Empty();

        using var document = JsonDocument.Parse(File.ReadAllBytes(file));

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return Empty();

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var row in document.RootElement.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
                Flatten(row, $"{sectionName}:{index.ToString(CultureInfo.InvariantCulture)}", values);

            index++;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    // Nested objects and arrays become colon-delimited keys, which is what IConfiguration's own
    // JSON provider does - so a consumer reading a section here cannot tell it did not come from
    // appsettings, and nothing downstream had to change to accept it.
    private static void Flatten(JsonElement element, string prefix, IDictionary<string, string?> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    Flatten(property.Value, $"{prefix}:{property.Name}", values);
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, $"{prefix}:{index.ToString(CultureInfo.InvariantCulture)}", values);
                    index++;
                }

                break;

            case JsonValueKind.Null:
                values[prefix] = null;
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                values[prefix] = element.GetBoolean() ? "true" : "false";
                break;

            default:
                values[prefix] = element.ToString();
                break;
        }
    }

    private static IConfiguration Empty() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase))
            .Build();
}
