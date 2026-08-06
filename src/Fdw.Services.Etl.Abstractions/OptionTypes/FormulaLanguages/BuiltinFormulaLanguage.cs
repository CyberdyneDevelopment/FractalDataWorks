using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The in-process built-in expression evaluator (arithmetic/string/field-reference).</summary>
[TypeOption(typeof(FormulaLanguages), "Builtin")]
public sealed class BuiltinFormulaLanguage : FormulaLanguageBase
{
    /// <summary>Initializes a new instance of the <see cref="BuiltinFormulaLanguage"/> class.</summary>
    public BuiltinFormulaLanguage() : base(1, "Builtin", isBuiltin: true)
    {
    }
}
