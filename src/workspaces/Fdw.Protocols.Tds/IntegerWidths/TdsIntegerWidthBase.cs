using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>The base of every width an INTN value is written in.</summary>
[ExcludeFromCodeCoverage]
public abstract class TdsIntegerWidthBase : TypeOptionBase<TdsIntegerWidthBase>, ITdsIntegerWidth
{
    /// <summary>Initializes a new instance of the <see cref="TdsIntegerWidthBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="width">The number of bytes the value takes.</param>
    protected TdsIntegerWidthBase(string name, int width)
        : base(name)
    {
        Width = width;
    }

    /// <inheritdoc/>
    [TypeLookup("ByWidth")]
    public int Width { get; }

    /// <inheritdoc/>
    public abstract object Read(TdsPayloadReader reader);
}
