using Fdw.Collections;

namespace Fdw.Protocols.Tds;

/// <summary>One width an INTN value is written in, and how a value of that width is read.</summary>
public interface ITdsIntegerWidth : ITypeOption<int, TdsIntegerWidthBase>
{
    /// <summary>Gets the number of bytes the value takes.</summary>
    int Width { get; }

    /// <summary>Reads a value of this width.</summary>
    /// <param name="reader">The reader, at the first byte of the value.</param>
    /// <returns>The value, in the .NET integer type of that width.</returns>
    object Read(TdsPayloadReader reader);
}
