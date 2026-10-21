using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="PredicateOperators"/>, using the CRTP pattern.
/// </summary>
public abstract class PredicateOperatorBase : TypeOptionBase<PredicateOperatorBase>, IPredicateOperator
{
    /// <summary>Initializes a new instance for the Empty sentinel: an operator with no forms and no example.</summary>
    protected PredicateOperatorBase()
        : base("NotFound", "Not found", "Unknown condition operator", "PredicateOperators")
    {
        Symbols = [];
        Example = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="PredicateOperatorBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="symbols">The forms the language reads for the operator; the first is shown as the operator's label.</param>
    /// <param name="meaning">What the operator asks of a record.</param>
    /// <param name="category">The group the console shows it under.</param>
    /// <param name="example">A complete condition that uses the operator.</param>
    protected PredicateOperatorBase(string name, IReadOnlyList<string> symbols, string meaning, string category, string example)
        : base(name, string.Join(" / ", symbols ?? throw new ArgumentNullException(nameof(symbols))), string.Concat(meaning, " Example: ", example), category)
    {
        if (symbols.Count == 0)
        {
            throw new ArgumentException("An operator states at least one form.", nameof(symbols));
        }

        Symbols = [.. symbols];
        Example = example ?? throw new ArgumentNullException(nameof(example));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Symbols { get; }

    /// <inheritdoc />
    public string Example { get; }
}
