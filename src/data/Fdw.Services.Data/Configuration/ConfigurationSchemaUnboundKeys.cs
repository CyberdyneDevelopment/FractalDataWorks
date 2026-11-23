using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Fdw.Messages;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Finds the keys of <c>configurationSchema.json</c> that no class binds (FDW-6584). The binder drops such a key without a word, so a host that states a value
/// nobody reads starts and runs as if the line were not there; this reads the same document a second time and says each one.
/// </summary>
/// <remarks>
/// A bound member is a settable property the binder itself knows (its own names, its aliases, its case rule), so the judge never disagrees with the binder.
/// A connection or secret manager entry's <c>Configuration</c> is judged against the type its <c>Implementation</c> names, the lookup its converter makes;
/// an entry whose kind nobody registered is left to that converter's own refusal. The keys of a dictionary are free.
/// A key is reported once for each parent it sits under: the first place, in the file's own words, and how many more places under the same parent state it.
/// </remarks>
internal static class ConfigurationSchemaUnboundKeys
{
    private const string EntryNameKey = "Name";
    private const string EntryImplementationKey = "Implementation";
    private const string EntryConfigurationKey = "Configuration";

    /// <summary>Judges the document against the classes it binds to.</summary>
    /// <param name="document">The root of the file, placeholders resolved.</param>
    /// <param name="schemaPath">The path that was read.</param>
    /// <param name="options">The options the file was bound with, so the binder's own member names decide.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>One message for each key and parent; empty when every key is bound.</returns>
    internal static IReadOnlyList<IGenericMessage> Judge(JsonElement document, string schemaPath, JsonSerializerOptions options, ILogger logger)
    {
        var found = new List<Unbound>();
        Walk(document, typeof(ConfigurationSchemaRoot), [], options, found);

        return
        [
            .. found
                .GroupBy(unbound => string.Concat(unbound.Key, "\u0001", ParentOf(unbound.Place)), StringComparer.Ordinal)
                .Select(group => ConfigurationGatewayProviderLog.SchemaKeyNotBound(
                    logger,
                    schemaPath,
                    group.First().Key,
                    Say(group.First().Place),
                    group.Count() > 1 ? $", and {group.Count() - 1} further places under the same parent" : string.Empty)),
        ];
    }

    private static void Walk(JsonElement element, Type type, IReadOnlyList<Segment> place, JsonSerializerOptions options, List<Unbound> found)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (EntryTypeOf(type) is { } entryKind)
        {
            WalkEntry(element, entryKind, place, options, found);
            return;
        }

        var info = options.GetTypeInfo(type);
        if (info.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            var bound = info.Properties.FirstOrDefault(candidate =>
                candidate.Set is not null && string.Equals(candidate.Name, property.Name, StringComparison.OrdinalIgnoreCase));

            if (bound is null)
            {
                found.Add(new Unbound(property.Name, place));
                continue;
            }

            WalkValue(property.Value, bound.PropertyType, property.Name, type == typeof(ConfigurationSchemaRoot), place, options, found);
        }
    }

    private static void WalkValue(JsonElement value, Type type, string name, bool isRoot, IReadOnlyList<Segment> place, JsonSerializerOptions options, List<Unbound> found)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            Walk(value, type, isRoot ? place : [.. place, new Segment(Words(name), null)], options, found);
            return;
        }

        if (value.ValueKind != JsonValueKind.Array || ElementTypeOf(type) is not { } elementType)
            return;

        var word = Singular(Words(name));
        var position = 0;
        foreach (var item in value.EnumerateArray())
        {
            position++;
            if (item.ValueKind == JsonValueKind.Object)
                Walk(item, elementType, [.. place, new Segment(word, NameOf(item, position), IsElement: true)], options, found);
        }
    }

    /// <summary>A connection or secret manager entry: its own three keys, then its Configuration against the type its Implementation names.</summary>
    private static void WalkEntry(JsonElement entry, Func<string, Type?> configurationTypeOf, IReadOnlyList<Segment> place, JsonSerializerOptions options, List<Unbound> found)
    {
        foreach (var property in entry.EnumerateObject())
        {
            var known = new[] { EntryNameKey, EntryImplementationKey, EntryConfigurationKey }
                .Any(key => string.Equals(key, property.Name, StringComparison.OrdinalIgnoreCase));
            if (!known)
                found.Add(new Unbound(property.Name, place));
        }

        if (Find(entry, EntryImplementationKey) is { ValueKind: JsonValueKind.String } implementation
            && implementation.GetString() is { Length: > 0 } kind
            && configurationTypeOf(kind) is { } configurationType
            && Find(entry, EntryConfigurationKey) is { } configuration)
        {
            Walk(configuration, configurationType, [.. place, new Segment("configuration", null)], options, found);
        }
    }

    /// <summary>For the two entry types a converter binds, the lookup of the concrete configuration type from the entry's Implementation; null for every other type.</summary>
    private static Func<string, Type?>? EntryTypeOf(Type type)
    {
        if (type == typeof(IConnectionImplementationConfiguration))
        {
            return kind => ConfigurationTypeOf(ConnectionTypes.ByName(kind), ConnectionTypes.NotFound, option => option.ConfigurationType);
        }

        if (type == typeof(ISecretManagerImplementationConfiguration))
        {
            return kind => ConfigurationTypeOf(SecretManagerTypes.ByName(kind), SecretManagerTypes.NotFound, option => option.ConfigurationType);
        }

        return null;
    }

    /// <summary>The configuration type of the option a kind names, or null when nobody registered that kind (the entry's converter refuses it with its own code).</summary>
    private static Type? ConfigurationTypeOf<TOption>(TOption option, TOption notFound, Func<TOption, Type?> configurationType)
        where TOption : class
        => ReferenceEquals(option, notFound) ? null : configurationType(option);

    private static Type? ElementTypeOf(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    /// <summary>The name an element states, and for an element that states none its place in the list (the first is 1), which is the file's own way of saying which.</summary>
    private static string NameOf(JsonElement item, int position)
        => Find(item, EntryNameKey) is { ValueKind: JsonValueKind.String } name && name.GetString() is { Length: > 0 } stated
            ? stated
            : position.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static JsonElement? Find(JsonElement entry, string key)
    {
        foreach (var property in entry.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    /// <summary>The words of a property name, lower case: <c>DataStores</c> is <c>data stores</c>.</summary>
    private static string Words(string name)
    {
        var words = new StringBuilder();
        foreach (var letter in name)
        {
            if (char.IsUpper(letter) && words.Length > 0)
                words.Append(' ');

            words.Append(char.ToLowerInvariant(letter));
        }

        return words.ToString();
    }

    /// <summary>The one a plural stands for: <c>data stores</c> is <c>data store</c>, <c>policies</c> is <c>policy</c>.</summary>
    private static string Singular(string words)
    {
        if (words.EndsWith("ies", StringComparison.Ordinal))
            return string.Concat(words.AsSpan(0, words.Length - 3), "y");

        return words.EndsWith('s') && !words.EndsWith("ss", StringComparison.Ordinal) ? words[..^1] : words;
    }

    private static string Say(IReadOnlyList<Segment> place)
        => string.Join(", ", place.Select(segment => segment.Name is null ? segment.Word : $"{segment.Word} '{segment.Name}'"));

    /// <summary>The place a key sits under: for a key on a list element, the parent of that element, so that many alike are one message.</summary>
    private static string ParentOf(IReadOnlyList<Segment> place)
        => Say(place.Count > 0 && place[^1].IsElement ? place.Take(place.Count - 1).ToList() : place);

    private sealed record Segment(string Word, string? Name, bool IsElement = false);

    private sealed record Unbound(string Key, IReadOnlyList<Segment> Place);
}
