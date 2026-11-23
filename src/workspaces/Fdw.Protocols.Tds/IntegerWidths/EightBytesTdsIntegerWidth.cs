using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>A value of 8 bytes.</summary>
[TypeOption(typeof(TdsIntegerWidths), "EightBytes")]
[ExcludeFromCodeCoverage]
public sealed class EightBytesTdsIntegerWidth : TdsIntegerWidthBase
{
    /// <summary>Initializes a new instance of the <see cref="EightBytesTdsIntegerWidth"/> class.</summary>
    public EightBytesTdsIntegerWidth()
        : base("EightBytes", 8)
    {
    }

    /// <inheritdoc/>
    public override object Read(TdsPayloadReader reader) => reader.ReadInt64();
}
