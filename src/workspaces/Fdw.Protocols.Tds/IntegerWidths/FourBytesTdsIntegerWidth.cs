using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>A value of 4 bytes.</summary>
[TypeOption(typeof(TdsIntegerWidths), "FourBytes")]
[ExcludeFromCodeCoverage]
public sealed class FourBytesTdsIntegerWidth : TdsIntegerWidthBase
{
    /// <summary>Initializes a new instance of the <see cref="FourBytesTdsIntegerWidth"/> class.</summary>
    public FourBytesTdsIntegerWidth()
        : base("FourBytes", 4)
    {
    }

    /// <inheritdoc/>
    public override object Read(TdsPayloadReader reader) => reader.ReadInt32();
}
