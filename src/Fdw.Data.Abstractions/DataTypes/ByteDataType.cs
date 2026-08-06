using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 8-bit unsigned integer values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>tinyint</c>, C# <see cref="byte"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Byte")]
public sealed class ByteDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteDataType"/> class.</summary>
    public ByteDataType()
        : base(id: 4, name: "Byte")
    {
    }
}
