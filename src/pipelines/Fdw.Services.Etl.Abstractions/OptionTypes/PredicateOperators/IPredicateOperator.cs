using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// One sign, word or form of the pipeline's condition language: what a filter expression, a validate check or a flow
/// condition is written with (FDW-1395).
/// </summary>
public interface IPredicateOperator : ITypeOption<int, IPredicateOperator>
{
    /// <summary>Gets the forms the language reads for the operator, as they are typed (for example <c>and</c> and <c>&amp;&amp;</c>).</summary>
    IReadOnlyList<string> Symbols { get; }

    /// <summary>Gets a complete condition that uses the operator and that the language reads.</summary>
    string Example { get; }
}
