using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Collection of formula languages available to Calculate transforms. Extensible — a consuming
/// assembly can register an additional language (e.g. a scripting engine) via module initializer;
/// the runtime requires an <see cref="IExpressionEvaluator"/> on the transform context for anything
/// other than <c>Builtin</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(FormulaLanguageBase), typeof(IFormulaLanguage), typeof(FormulaLanguages))]
public abstract partial class FormulaLanguages : TypeCollectionBase<FormulaLanguageBase, IFormulaLanguage>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
