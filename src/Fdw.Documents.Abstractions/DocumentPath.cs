using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Documents.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Documents.Abstractions;

/// <summary>
/// A path to a document, relative to the scope of a connection (FDW-1498): segments joined by one forward slash, with no leading
/// or trailing slash, no empty segment and no parent segment. The empty path is the scope itself.
/// </summary>
/// <remarks>
/// Why parse and not wrap a string: whatever reaches a storage connection has been made into one of these first, so a path that
/// climbs out of the scope (<c>..</c>) never exists as a value. Separators are normalised: a backslash is a slash, and a run of
/// slashes is one.
/// </remarks>
public readonly struct DocumentPath : IEquatable<DocumentPath>
{
    private readonly string? _value;

    private DocumentPath(string value)
    {
        _value = value;
    }

    /// <summary>Gets the path of the scope itself: no segments.</summary>
    public static DocumentPath Root => default;

    /// <summary>Gets the path as text: segments joined by a forward slash, empty for the root.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>Gets whether this is the root.</summary>
    public bool IsRoot => Value.Length == 0;

    /// <summary>Gets the segments, in order.</summary>
    public IReadOnlyList<string> Segments => Value.Length == 0 ? [] : Value.Split('/');

    /// <summary>Gets the last segment, which is a document's name; empty for the root.</summary>
    public string Name => IsRoot ? string.Empty : Value.Substring(Value.LastIndexOf('/') + 1);

    /// <summary>Reads text as a document path.</summary>
    /// <param name="text">The text; null or empty is the root.</param>
    /// <returns>The path, or the failure that names what is wrong with the text.</returns>
    public static IGenericResult<DocumentPath> Parse(string? text) => DocumentPathParser.Parse(text);

    /// <summary>Joins another path below this one.</summary>
    /// <param name="below">The path to put below this one.</param>
    /// <returns>The joined path.</returns>
    public DocumentPath Combine(DocumentPath below) =>
        IsRoot ? below : below.IsRoot ? this : new DocumentPath(Value + "/" + below.Value);

    /// <summary>Says whether this path is the other path or lies below it, by whole segments: <c>a/bc</c> is not below <c>a/b</c>.</summary>
    /// <param name="ancestor">The path that may contain this one.</param>
    /// <returns>True when this path is inside the other.</returns>
    public bool IsWithin(DocumentPath ancestor) =>
        ancestor.IsRoot
        || string.Equals(Value, ancestor.Value, StringComparison.Ordinal)
        || Value.StartsWith(ancestor.Value + "/", StringComparison.Ordinal);

    /// <summary>Makes a path from segments already checked by the parser.</summary>
    /// <param name="value">The normalised text.</param>
    /// <returns>The path.</returns>
    internal static DocumentPath FromNormalised(string value) => new(value);

    /// <inheritdoc />
    public bool Equals(DocumentPath other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DocumentPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Compares two paths.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True when they are the same path.</returns>
    public static bool operator ==(DocumentPath left, DocumentPath right) => left.Equals(right);

    /// <summary>Compares two paths.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True when they differ.</returns>
    public static bool operator !=(DocumentPath left, DocumentPath right) => !left.Equals(right);
}
