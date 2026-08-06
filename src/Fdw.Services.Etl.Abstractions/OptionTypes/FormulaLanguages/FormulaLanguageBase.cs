using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for formula language type options using the CRTP pattern.
/// </summary>
public abstract class FormulaLanguageBase : TypeOptionBase<int, FormulaLanguageBase>, IFormulaLanguage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaLanguageBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The language name (e.g., "Builtin").</param>
    /// <param name="isBuiltin">Whether this language is evaluated by the in-process built-in evaluator.</param>
    protected FormulaLanguageBase(int id, string name, bool isBuiltin) : base(id, name, "FormulaLanguages")
    {
        IsBuiltin = isBuiltin;
    }

    /// <inheritdoc/>
    public bool IsBuiltin { get; }
}
