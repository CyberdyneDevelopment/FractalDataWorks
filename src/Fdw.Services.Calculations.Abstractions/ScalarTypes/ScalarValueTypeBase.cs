using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for scalar value types (CRTP pattern).
/// </summary>
public abstract class ScalarValueTypeBase : TypeOptionBase<ScalarValueTypeBase>, IScalarValueType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScalarValueTypeBase"/> class.
    /// </summary>
    /// <param name="name">The name of this scalar value type.</param>
    protected ScalarValueTypeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A type with no format takes any text, the empty text included: that is a String or a CharArray. A type with a format overrides
    /// this and says which text it takes.
    /// </remarks>
    public virtual bool IsValidText(string serializedValue) => serializedValue is not null;
}
