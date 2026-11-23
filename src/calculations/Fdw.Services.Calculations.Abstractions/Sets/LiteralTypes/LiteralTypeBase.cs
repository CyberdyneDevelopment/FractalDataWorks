using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>The base of every literal type: a type of stated value a literal column can hold.</summary>
[ExcludeFromCodeCoverage]
public abstract class LiteralTypeBase : TypeOptionBase<LiteralTypeBase>, ILiteralType
{
    /// <summary>Initializes a new instance of the <see cref="LiteralTypeBase"/> class.</summary>
    /// <param name="name">The type's name, the name a literal column is stored and chosen by.</param>
    protected LiteralTypeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object? value);

    /// <inheritdoc />
    public abstract string Write(object value);

    /// <inheritdoc />
    public abstract object? Read(string text);
}
