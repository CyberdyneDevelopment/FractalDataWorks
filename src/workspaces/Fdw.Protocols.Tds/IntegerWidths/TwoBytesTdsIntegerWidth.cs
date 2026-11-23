using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>A value of 2 bytes.</summary>
[TypeOption(typeof(TdsIntegerWidths), "TwoBytes")]
[ExcludeFromCodeCoverage]
public sealed class TwoBytesTdsIntegerWidth : TdsIntegerWidthBase
{
    /// <summary>Initializes a new instance of the <see cref="TwoBytesTdsIntegerWidth"/> class.</summary>
    public TwoBytesTdsIntegerWidth()
        : base("TwoBytes", 2)
    {
    }

    /// <inheritdoc/>
    public override object Read(TdsPayloadReader reader) => reader.ReadInt16();
}
