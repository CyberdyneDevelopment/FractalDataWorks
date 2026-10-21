using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for a vector: a fixed-length list of single-precision numbers, the kind a model's embedding of a text is.
/// </summary>
/// <remarks>
/// Maps to: SQL Server 2025 <c>vector(n)</c>, C# <c>float[]</c>. Its text is a JSON array of numbers,
/// <c>[0.6,0.8,0]</c>, which SQL Server converts to and from a vector (FDW-1354).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Vector")]
public sealed class VectorDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VectorDataType"/> class.</summary>
    public VectorDataType()
        : base(name: "Vector")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(float[]);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Vector(text);

    /// <inheritdoc />
    /// <remarks>A vector is compared by distance, never written into an expression as a literal.</remarks>
    public override bool RendersInExpressions => false;

    /// <inheritdoc />
    public override string Format(object value) => DataValueReader.VectorText((float[])value);
}
