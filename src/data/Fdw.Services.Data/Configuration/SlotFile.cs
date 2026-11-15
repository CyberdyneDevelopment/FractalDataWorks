using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// The file an A/B slot's host is started with (<c>APP_SLOT_FILE</c>, <c>/var/lib/app-slots/&lt;app&gt;/slot-a.json</c> or
/// <c>slot-b.json</c>), read as one more source of values beside the environment (FDW-5845).
/// </summary>
/// <remarks>
/// The file's scalar values are addressed by the path of their keys joined with <c>__</c>, the same spelling .NET
/// configuration gives a nested key: <c>databases__TechComDb</c>, <c>ports__tds</c>, <c>database_server</c>,
/// <c>services__etl__address</c>, <c>hostnames__live__0</c> (an array's items are numbered from 0). A <c>null</c> or an
/// empty string is not stated, as an empty environment variable is not. A host that does not run on a slot has no
/// <c>APP_SLOT_FILE</c>, and <see cref="DeclaredPath"/> answers that nothing is declared: no file is read and nothing
/// is assumed about one.
/// </remarks>
public sealed class SlotFile
{
    /// <summary>The environment variable that names the slot file.</summary>
    public const string EnvironmentVariableName = "APP_SLOT_FILE";

    /// <summary>The separator that joins the keys of a nested value into one name.</summary>
    public const string PathSeparator = "__";

    private SlotFile(string filePath, IReadOnlyDictionary<string, string> values)
    {
        FilePath = filePath;
        Values = values;
    }

    /// <summary>Gets the path the file was read from.</summary>
    public string FilePath { get; }

    /// <summary>Gets every value the file states, by the name its key path gives it.</summary>
    public IReadOnlyDictionary<string, string> Values { get; }

    /// <summary>Reads the path <c>APP_SLOT_FILE</c> names.</summary>
    /// <returns>The path, or <see langword="null"/> when the host does not declare that it runs on a slot (the variable is unset or empty).</returns>
    public static string? DeclaredPath()
        => Environment.GetEnvironmentVariable(EnvironmentVariableName) is { Length: > 0 } declared ? declared : null;

    /// <summary>Reads and flattens the slot file at <paramref name="filePath"/>.</summary>
    /// <param name="filePath">The path <c>APP_SLOT_FILE</c> names.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The values the file states, or why it cannot be read.</returns>
    public static IGenericResult<SlotFile> Read(string filePath, ILogger logger)
    {
        string text;
        try
        {
            text = File.ReadAllText(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return GenericResult<SlotFile>.Failure(SlotFileLog.Unreadable(logger, filePath, ex.Message));
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException ex)
        {
            return GenericResult<SlotFile>.Failure(SlotFileLog.NotAJsonObject(logger, filePath, ex.Message));
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return GenericResult<SlotFile>.Failure(SlotFileLog.NotAJsonObject(logger, filePath, $"the top-level value is a {document.RootElement.ValueKind}"));

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (Flatten(document.RootElement, string.Empty, values) is { } twice)
                return GenericResult<SlotFile>.Failure(SlotFileLog.NameStatedTwice(logger, filePath, twice));

            SlotFileLog.Read(logger, filePath, values.Count);
            return GenericResult<SlotFile>.Success(new SlotFile(filePath, values));
        }
    }

    private static string? Flatten(JsonElement element, string name, Dictionary<string, string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return FlattenObject(element, name, values);
            case JsonValueKind.Array:
                return FlattenArray(element, name, values);
            case JsonValueKind.String:
                return element.GetString() is { Length: > 0 } text ? Add(values, name, text) : null;
            case JsonValueKind.Number:
                return Add(values, name, element.GetRawText());
            case JsonValueKind.True:
                return Add(values, name, "true");
            case JsonValueKind.False:
                return Add(values, name, "false");
            default:
                return null;
        }
    }

    private static string? FlattenObject(JsonElement element, string name, Dictionary<string, string> values)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (Flatten(property.Value, name.Length == 0 ? property.Name : name + PathSeparator + property.Name, values) is { } twice)
                return twice;
        }

        return null;
    }

    private static string? FlattenArray(JsonElement element, string name, Dictionary<string, string> values)
    {
        var index = 0;
        foreach (var item in element.EnumerateArray())
        {
            if (Flatten(item, name + PathSeparator + index.ToString(CultureInfo.InvariantCulture), values) is { } twice)
                return twice;
            index++;
        }

        return null;
    }

    private static string? Add(Dictionary<string, string> values, string name, string value)
        => values.TryAdd(name, value) ? null : name;
}
