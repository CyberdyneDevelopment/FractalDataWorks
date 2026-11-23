using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>A value of 1 byte.</summary>
[TypeOption(typeof(TdsIntegerWidths), "OneByte")]
[ExcludeFromCodeCoverage]
public sealed class OneByteTdsIntegerWidth : TdsIntegerWidthBase
{
    /// <summary>Initializes a new instance of the <see cref="OneByteTdsIntegerWidth"/> class.</summary>
    public OneByteTdsIntegerWidth()
        : base("OneByte", 1)
    {
    }

    /// <inheritdoc/>
    public override object Read(TdsPayloadReader reader) => reader.ReadByte();
}
