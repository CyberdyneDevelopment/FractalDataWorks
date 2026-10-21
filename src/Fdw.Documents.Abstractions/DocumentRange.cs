using System;
using System.Runtime.InteropServices;

namespace Fdw.Documents.Abstractions;

/// <summary>A byte range of a document: where it starts and how many bytes (FDW-1497).</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct DocumentRange : IEquatable<DocumentRange>
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRange"/> struct.</summary>
    /// <param name="offset">The byte the range starts at; not negative.</param>
    /// <param name="length">How many bytes; at least one.</param>
    public DocumentRange(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }

    /// <summary>Gets the byte the range starts at.</summary>
    public long Offset { get; }

    /// <summary>Gets how many bytes the range holds.</summary>
    public long Length { get; }

    /// <summary>Gets whether the range starts at zero or later and holds at least one byte.</summary>
    public bool IsValid => Offset >= 0 && Length >= 1;

    /// <inheritdoc />
    public bool Equals(DocumentRange other) => Offset == other.Offset && Length == other.Length;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DocumentRange other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (Offset.GetHashCode() * 397) ^ Length.GetHashCode();

    /// <summary>Compares two ranges.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True when they are the same range.</returns>
    public static bool operator ==(DocumentRange left, DocumentRange right) => left.Equals(right);

    /// <summary>Compares two ranges.</summary>
    /// <param name="left">The first.</param>
    /// <param name="right">The second.</param>
    /// <returns>True when they differ.</returns>
    public static bool operator !=(DocumentRange left, DocumentRange right) => !left.Equals(right);
}
